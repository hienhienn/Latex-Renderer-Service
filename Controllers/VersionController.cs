using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LatexRendererAPI.Data;
using LatexRendererAPI.Models.DTO;
using LatexRendererAPI.Models.Domain;
using LatexRendererAPI.Services;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;

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

    public VersionController(AppDbContext _dbContext, IConfiguration _config, IFileService _fileService)
    {
      dbContext = _dbContext;
      config = _config;
      fileService = _fileService;
    }

    [HttpPost]
    [Route("compile")]
    public IActionResult CompilePDF([FromBody] CompileDto dto)
    {   
      try {
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), config["AssetPath"] ?? "", dto.Code);
        Parallel.ForEach(dto.Files, f => {
          var folderFilePath = fileService.ParseFolderPath(f.Path, dto.Code, "file");
          Directory.CreateDirectory(folderFilePath);
          if(f.Type == "tex")
          {
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(folderFilePath, f.Name)))
            {
              outputFile.WriteAsync(f.Content);
            }
          } 
          else 
          {
            var sourcePath = Path.Combine(Directory.GetCurrentDirectory(), config["AssetPath"] ?? "", f.Content ?? "");
            var desPath = Path.Combine(folderFilePath, f.Name);
            fileService.CopyFile(sourcePath, desPath);
          }
        });

        var mainFile = new FileInfo(Path.Combine(folderPath, "main.tex"));

        if(!mainFile.Exists) {
          return BadRequest(new {
            message = "Main.tex file does not exists in this projects"
          });
        }
        var processInfo = new ProcessStartInfo("cmd.exe", "/c " + "pdflatex main.tex")
        {
          UseShellExecute = false,
          WorkingDirectory = folderPath,
        };
        var process = Process.Start(processInfo);
        process?.WaitForExit();
        return Ok($"/{dto.Code}/main.pdf");
      } catch {
        return BadRequest();
      }
    }

    [HttpDelete]
    [Route("compile/{Code}")]
    public IActionResult DeleteCompileFolder([FromRoute] string Code) {
      try {
        fileService.DeleteFolder(Path.Combine(Directory.GetCurrentDirectory(), config["AssetPath"] ?? "", Code));
        return Ok();
      } catch {
        return NotFound();
      }
    }
    
    [HttpPost]
    [Route("downloadFolder")]
    public IActionResult DownloadFolder([FromBody] DownloadFolderDto dto)
    {
      Parallel.ForEach(dto.Files, f => {
        var folderFilePath = fileService.ParseFolderPath(f.Path, dto.Code, "file");
        Directory.CreateDirectory(folderFilePath);
        if(f.Type == "tex")
        {
          using (StreamWriter outputFile = new StreamWriter(Path.Combine(folderFilePath, f.Name)))
          {
            outputFile.WriteAsync(f.Content);
          }
        } 
        else 
        {
          var sourcePath = Path.Combine(Directory.GetCurrentDirectory(), config["AssetPath"] ?? "", f.Content ?? "");
          var desPath = Path.Combine(folderFilePath, f.Name);
          fileService.CopyFile(sourcePath, desPath);
        }
      });
    
      var zipFolderPath = fileService.ParseFolderPath(dto.FolderPath, dto.Code, "folder");
      var zipFilePath = Path.Combine(
        Directory.GetCurrentDirectory(), 
        config["AssetPath"] ?? "", 
        $"{dto.FolderName}-{DateTime.Now.Ticks}.zip"
      );
      ZipFile.CreateFromDirectory(zipFolderPath, zipFilePath);
      byte[] fileBytes = System.IO.File.ReadAllBytes(zipFilePath);
      return File(fileBytes, "application/zip", dto.FolderName);
      // fileService.DeleteFile(zipFilePath);
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
        Description = dto.Description
      };
      dbContext.Add(version);
      Parallel.ForEach(dto.Files, f => {
         var newFile = new FileModel
          {
            Content = f.Content,
            Type = f.Type,
            Path = f.Path,
            Name = f.Name,
            VersionId = version.Id
          };
        dbContext.Files.AddAsync(newFile);
      });
      dbContext.SaveChanges();
      return Ok();
    }

    [HttpGet]
    [Route("{id:Guid}")]
    public IActionResult getVersionById([FromRoute] Guid id)
    {
      var version = dbContext.Versions.FirstOrDefault(v => v.Id == id);

      if (version == null) return NotFound();

      var project = dbContext.Projects
      .Include(p => p.Versions)
      // .Include(p => p.Owner)
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
            v.Description
          }),
          p.IsPublic,
          UserProjects = p.UserProjects.Select(up => new 
          {
            up.Role,
            up.Editor.Fullname,
            up.Editor.Username,
            up.EditorId,
            up.Id
          }),
          version.IsMainVersion
        }
      )
      .First(v => v.Id == version.ProjectId)
      ;
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
  }
}