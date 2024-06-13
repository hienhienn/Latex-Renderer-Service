using System.Diagnostics;
using System.IO.Compression;
using LatexRendererAPI.Data;
using LatexRendererAPI.Models.Domain;
using LatexRendererAPI.Models.DTO;
using LatexRendererAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LatexRendererAPI.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        private AppDbContext dbContext;
        private IConfiguration config;
        private IFileService fileService;

        public VersionController(
            AppDbContext _dbContext,
            IConfiguration _config,
            IFileService _fileService
        )
        {
            dbContext = _dbContext;
            config = _config;
            fileService = _fileService;
        }

        [HttpPost]
        [Route("compile")]
        public IActionResult CompilePDF([FromBody] CompileDto dto)
        {
            var folderPath = fileService.ParseFolderPath(dto.CompilePath, dto.Code, "file");
            var name = dto.CompilePath.Split('/').Last();
            var filePath = Path.Combine(folderPath, name);
            var nameWoExt = Path.GetFileNameWithoutExtension(filePath);
            var tasks = dto
                .Files.Select(async f =>
                {
                    var folderFilePath = fileService.ParseFolderPath(f.Path, dto.Code, "file");
                    Directory.CreateDirectory(folderFilePath);
                    if (f.Type == "tex")
                    {
                        var filePath = Path.Combine(folderFilePath, f.Name);
                        using (StreamWriter outputFile = new StreamWriter(filePath))
                        {
                            await outputFile.WriteAsync(f.Content);
                        }
                    }
                    else
                    {
                        var sourcePath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            config["AssetPath"] ?? "",
                            f.Content ?? ""
                        );
                        var desPath = Path.Combine(folderFilePath, f.Name);
                        fileService.CopyFile(sourcePath, desPath);
                    }
                })
                .ToArray();

            Task.WhenAll(tasks).Wait();

            var processInfo = new ProcessStartInfo("cmd.exe", $"/c pdflatex {name}")
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                WorkingDirectory = folderPath,
            };
            var process = Process.Start(processInfo);
            Task.Run(() =>
            {
                using (StreamWriter sw = process.StandardInput)
                {
                    while (!process.HasExited)
                    {
                        if (sw.BaseStream.CanWrite)
                        {
                            sw.WriteLine();
                        }
                    }
                }
            });
            process?.WaitForExit();
            if (fileService.CheckExists(Path.Combine(folderPath, nameWoExt + ".pdf")))
            {
                return Ok(new { CompileSuccess = true, Path = dto.CompilePath });
            }
            return Ok(new { CompileSuccess = false });
        }

        [HttpDelete]
        [Route("compile/{Code}")]
        public IActionResult DeleteCompileFolder([FromRoute] string Code)
        {
            try
            {
                fileService.DeleteFolder(
                    Path.Combine(Directory.GetCurrentDirectory(), config["AssetPath"] ?? "", Code)
                );
                return Ok();
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("downloadFolder")]
        public IActionResult DownloadFolder([FromBody] DownloadFolderDto dto)
        {
            Parallel.ForEach(
                dto.Files,
                f =>
                {
                    var folderFilePath = fileService.ParseFolderPath(f.Path, dto.Code, "file");
                    Directory.CreateDirectory(folderFilePath);
                    if (f.Type == "tex")
                    {
                        using (
                            StreamWriter outputFile = new StreamWriter(
                                Path.Combine(folderFilePath, f.Name)
                            )
                        )
                        {
                            outputFile.WriteAsync(f.Content);
                        }
                    }
                    else
                    {
                        var sourcePath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            config["AssetPath"] ?? "",
                            f.Content ?? ""
                        );
                        var desPath = Path.Combine(folderFilePath, f.Name);
                        fileService.CopyFile(sourcePath, desPath);
                    }
                }
            );

            var zipFolderPath = fileService.ParseFolderPath(
                dto.FolderPath ?? "",
                dto.Code,
                "folder"
            );
            var zipFilePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                config["AssetPath"] ?? "",
                $"{dto.FolderName}-{DateTime.Now.Ticks}.zip"
            );
            ZipFile.CreateFromDirectory(zipFolderPath, zipFilePath);
            byte[] fileBytes = System.IO.File.ReadAllBytes(zipFilePath);
            return File(fileBytes, "application/zip", dto.FolderName);
        }

        [HttpPost]
        [Route("saveVersion/{projectId:Guid}")]
        public IActionResult SaveVersion([FromRoute] Guid projectId, [FromBody] SaveVersionDto dto)
        {
            var currentUser = HttpContext.User;
            var userId = User.Claims.First(claim => claim.Type == "UserId").Value;

            var version = new VersionModel
            {
                ProjectId = projectId,
                ModifiedTime = DateTime.Now,
                EditorId = Guid.Parse(userId),
                IsMainVersion = false,
                Description = dto.Description,
                MainFileId = new Guid()
            };
            dbContext.Add(version);
            Parallel.ForEach(
                dto.Files,
                f =>
                {
                    var newFile = new FileModel
                    {
                        Content = f.Content,
                        Type = f.Type,
                        Path = f.Path,
                        Name = f.Name,
                        VersionId = version.Id
                    };
                    dbContext.Files.AddAsync(newFile);
                }
            );
            var mainFile = dbContext.Files.First(f =>
                f.Path == dto.MainFilePath && f.VersionId == version.Id
            );
            if (mainFile != null)
                version.MainFileId = mainFile.Id;
            dbContext.SaveChanges();
            return Ok();
        }

        [HttpGet]
        [Route("{id:Guid}")]
        public IActionResult getVersionById([FromRoute] Guid id)
        {
            var version = dbContext.Versions.FirstOrDefault(v => v.Id == id);

            if (version == null)
                return NotFound();

            var currentUser = HttpContext.User;
            var userId = User.Claims.First(claim => claim.Type == "UserId").Value;

            var project = dbContext
                .Projects.Include(p => p.Versions)
                .Select(p => new
                {
                    p.Name,
                    p.Id,
                    Versions = p.Versions.Select(v => new
                    {
                        v.Editor.Fullname,
                        v.Editor.Username,
                        v.IsMainVersion,
                        v.ModifiedTime,
                        v.Description,
                        v.Id
                    }),
                    p.IsPublic,
                    p.MainVersionId,
                    UserProjects = p.UserProjects.Select(up => new
                    {
                        up.Role,
                        up.Editor.Fullname,
                        up.Editor.Username,
                        up.EditorId,
                        up.Id
                    }),
                    version.IsMainVersion,
                    p.Versions.First(p => p.IsMainVersion == true).MainFileId,
                    Role = p.UserProjects.First(v => v.EditorId == Guid.Parse(userId)).Role ?? null,
                    userId,
                    TotalStar = p.StarProjects.Count(),
                    Starred = p.StarProjects.First(sp => sp.EditorId == Guid.Parse(userId)) != null
                        ? true
                        : false,
                    StarredId = p.StarProjects.First(sp => sp.EditorId == Guid.Parse(userId))
                    != null
                        ? p.StarProjects.First(sp => sp.EditorId == Guid.Parse(userId)).Id
                        : new Guid()
                })
                .First(v => v.Id == version.ProjectId);
            // var listVersion = dbContext.Versions
            //   .Where(v => v.ProjectId == version.ProjectId)
            //   .Include(v => v.Editor)
            //   .Select(v => new {
            //     v.Id,
            //     v.IsMainVersion,
            //     v.Editor,
            //     v.ModifiedTime,
            //     v.Description
            //   });
            // return Ok(new {
            //   version,
            //   listVersion
            // });
            return Ok(project);
        }

        [HttpPut]
        [Route("{id:Guid}")]
        public IActionResult UpdateVersion([FromRoute] Guid id, [FromBody] UpdateVersionDto dto)
        {
            var version = dbContext.Versions.Find(id);
            if (version == null)
                return NotFound();

            if (dto.MainFileId != null)
                version.MainFileId = Guid.Parse(dto.MainFileId);

            dbContext.SaveChanges();
            return Ok(dto);
        }
    }
}
