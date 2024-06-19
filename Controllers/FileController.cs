using System.Security.Cryptography;
using System.Text;
using LatexRendererAPI.Data;
using LatexRendererAPI.Models.Domain;
using LatexRendererAPI.Models.DTO;
using LatexRendererAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LatexRendererAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private AppDbContext dbContext;
        private IFileService fileService;
        private IConfiguration config;
        private AuthService authService;

        public FileController(
            AppDbContext _dbContext,
            IFileService _fileService,
            IConfiguration _config,
            AuthService _authService
        )
        {
            dbContext = _dbContext;
            fileService = _fileService;
            config = _config;
            authService = _authService;
        }

        [HttpGet]
        [Route("getAll/{versionId:Guid}")]
        public IActionResult GetFilesVersion([FromRoute] Guid versionId)
        {
            var version = dbContext.Versions.FirstOrDefault(v => v.Id == versionId);

            if (version == null)
                return NotFound();
            var currentUser = HttpContext.User;
            var userId = HttpContext.User.Identity.IsAuthenticated ? User.Claims.First(claim => claim.Type == "UserId").Value : new Guid().ToString();

            var project = dbContext.Projects.Include(p => p.UserProjects).First(p => p.Id == version.ProjectId);
            if (
                !project.IsPublic &&
                project.UserProjects.FirstOrDefault(up => up.EditorId == Guid.Parse(userId)) == null
            )
            {
                return Unauthorized();
            }

            var files = dbContext
                .Files.Where(f => f.VersionId == versionId)
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.Path,
                    f.Type,
                    f.Content,
                    f.ShaCode
                })
                .OrderBy(f => f.Path);

            return Ok(files);
        }

        [HttpGet]
        [Route("{id:Guid}")]
        public IActionResult GetFileById([FromRoute] Guid id)
        {
            var file = dbContext
                .Files.Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.Content,
                    f.Path,
                    f.ShaCode,
                    f.Type
                })
                .FirstOrDefault(f => f.Id == id);
            if (file == null)
            {
                return NotFound();
            }
            return Ok(file);
        }

        [HttpGet]
        [Route("download/{id:Guid}")]
        public IActionResult GetFileDownload([FromRoute] Guid id)
        {
            var file = dbContext.Files.Find(id);
            if (file == null)
                return NotFound();

            if (file.Type == "img")
            {
                var filePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    config["AssetPath"] ?? "",
                    file.Content
                );
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

                return File(fileBytes, "image/jpeg", file.Name);
            }

            return Ok();
        }

        [HttpPost]
        [Route("uploadFile")]
        [Authorize]
        public async Task<IActionResult> UploadImage(
            [FromForm] IFormFile file,
            [FromForm] string name,
            [FromForm] Guid versionId,
            [FromForm] string path
        )
        {
            var version = await dbContext.Versions.FirstOrDefaultAsync(p => p.Id == versionId);
            if (version == null)
                return NotFound();
            var existFile = dbContext.Files.FirstOrDefault(p => p.Path == path);
            if (existFile != null)
            {
                return BadRequest(new { message = "A file with this name already exists!" });
            }

            var filePath = await fileService.SaveFile(file, name, path);
            var fileModel = new FileModel
            {
                Name = name,
                Content = filePath,
                VersionId = versionId,
                Path = path,
                Type = "img",
            };
            dbContext.Add(fileModel);

            var currentUser = HttpContext.User;
            var userId = User.Claims.First(claim => claim.Type == "UserId").Value;
            version.ModifiedTime = DateTime.Now;
            version.EditorId = Guid.Parse(userId);

            await dbContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetFileById), new { id = fileModel.Id }, fileModel);
        }

        [HttpPost]
        [Route("createFile")]
        [Authorize]
        public IActionResult CreateFile([FromBody] CreateFileDto createFileDto)
        {
            if (ModelState.IsValid)
            {
                var version = dbContext.Versions.FirstOrDefault(p =>
                    p.Id == createFileDto.VersionId
                );
                if (version == null)
                    return NotFound();

                var existFile = dbContext.Files.FirstOrDefault(p => p.Path == createFileDto.Path);
                if (existFile != null)
                {
                    return BadRequest(new { message = "A file with this name already exists!" });
                }

                var fileModel = new FileModel
                {
                    Name = createFileDto.Name,
                    Content = createFileDto.Content ?? "",
                    VersionId = createFileDto.VersionId,
                    Path = createFileDto.Path,
                    Type = "tex"
                };
                dbContext.Add(fileModel);

                var currentUser = HttpContext.User;
                var userId = User.Claims.First(claim => claim.Type == "UserId").Value;
                version.ModifiedTime = DateTime.Now;
                version.EditorId = Guid.Parse(userId);

                dbContext.SaveChanges();
                return Ok();
            }
            return BadRequest(ModelState);
        }

        [HttpPut]
        [Route("updateFile/{id:Guid}")]
        [Authorize]

        public IActionResult UpdateFile([FromRoute] Guid id, [FromBody] UpdateFileDto dto)
        {
            if (ModelState.IsValid)
            {
                var file = dbContext.Files.FirstOrDefault(p => p.Id == id);
                if (file == null)
                {
                    return NotFound();
                }
                if (file.ShaCode != null && file.ShaCode != dto.ShaCode)
                    return BadRequest(
                        new
                        {
                            message = "ShaCode did not match",
                            shaCodeError = true,
                            file.Content,
                            file.ShaCode,
                            file.Id,
                            file.Path,
                            file.Name,
                            file.Type
                        }
                    );

                if (dto.Content != null)
                    file.Content = dto.Content;
                if (dto.Name != null)
                    file.Name = dto.Name;
                if (dto.Path != null)
                    file.Path = dto.Path;

                var filesStr =
                    $"Id: {file.Id}, Name: {file.Name}, Path: {file.Path}, Content{file.Content}";
                var newShaCode = ComputeSha256Hash(filesStr ?? "");
                file.ShaCode = newShaCode;
                var version = dbContext.Versions.Find(file.VersionId);
                if (version != null)
                {
                    var currentUser = HttpContext.User;
                    var userId = User.Claims.First(claim => claim.Type == "UserId").Value;

                    version.ModifiedTime = DateTime.Now;
                    version.EditorId = Guid.Parse(userId);
                }
                dbContext.SaveChanges();

                return Ok(new { id = file.Id, shaCode = newShaCode });
            }
            return BadRequest(ModelState);
        }

        [HttpPut]
        [Route("renameFile/{id:Guid}")]
        [Authorize]
        public IActionResult RenameFile([FromRoute] Guid id, [FromBody] UpdateFileDto dto)
        {
            if (ModelState.IsValid)
            {
                var file = dbContext.Files.FirstOrDefault(p => p.Id == id);
                if (file == null)
                {
                    return NotFound();
                }
                var existFile = dbContext.Files.FirstOrDefault(p => p.Path == dto.Path);
                if (existFile != null)
                {
                    return BadRequest(new { message = "A file with this name already exists!" });
                }

                if (dto.Name != null)
                    file.Name = dto.Name;
                if (dto.Path != null)
                    file.Path = dto.Path;

                var filesStr =
                    $"Id: {file.Id}, Name: {file.Name}, Path: {file.Path}, Content{file.Content}";
                var newShaCode = ComputeSha256Hash(filesStr ?? "");
                file.ShaCode = newShaCode;

                var version = dbContext.Versions.Find(file.VersionId);
                if (version != null)
                {
                    var currentUser = HttpContext.User;
                    var userId = User.Claims.First(claim => claim.Type == "UserId").Value;

                    version.ModifiedTime = DateTime.Now;
                    version.EditorId = Guid.Parse(userId);
                }

                dbContext.SaveChanges();

                return Ok(new { id = file.Id, shaCode = newShaCode });
            }
            return BadRequest(ModelState);
        }

        [HttpDelete]
        [Route("deleteFile/{id:Guid}")]
        [Authorize]
        public IActionResult DeleteFile([FromRoute] Guid id, [FromQuery] string shaCode)
        {
            var file = dbContext.Files.Find(id);
            if (file == null)
            {
                return NotFound();
            }
            if (file.Type == "tex" && file.ShaCode != null && file.ShaCode != shaCode)
            {
                return BadRequest(
                    new
                    {
                        message = $"File {file.Path} has been change before delete!",
                        shaCodeError = true,
                        file.ShaCode,
                        file.Id
                    }
                );
            }
            var version = dbContext.Versions.Find(file.VersionId);
            if (version == null)
                return NotFound();

            var currentUser = HttpContext.User;
            var userId = User.Claims.First(claim => claim.Type == "UserId").Value;

            version.ModifiedTime = DateTime.Now;
            version.EditorId = Guid.Parse(userId);

            dbContext.Files.Remove(file);
            dbContext.SaveChanges();

            return Ok();
        }

        private string ComputeSha256Hash(string text)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] textBytes = Encoding.UTF8.GetBytes(text);
                byte[] hashBytes = sha256.ComputeHash(textBytes);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
