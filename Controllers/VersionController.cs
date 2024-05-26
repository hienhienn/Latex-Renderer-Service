using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LatexRendererAPI.Data;
using LatexRendererAPI.Models.DTO;
using LatexRendererAPI.Models.Domain;
using LatexRendererAPI.Services;

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
        Directory.CreateDirectory(folderPath);
        Parallel.ForEach(dto.Files, f => {
          var folderFilePath = fileService.ParseFolderPath(f.Path, dto.Code);
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
            Console.WriteLine(sourcePath);
            Console.WriteLine(desPath);
            fileService.CopyFile(sourcePath, desPath);
            // using (FileStream sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
            // using (FileStream destinationStream = new FileStream(desPath, FileMode.Open, FileAccess.Write))
            // {
            //   byte[] buffer = new byte[1024];
            //   int bytesRead;

            //   while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
            //   {
            //     destinationStream.Write(buffer, 0, bytesRead);
            //   }
            // }
          }
        });

        var mainFile = new FileInfo(Path.Combine(folderPath, "main.tex"));

        if(!mainFile.Exists) {
          return BadRequest(new {
            message = "Main.tex file does not exists in this projects"
          });
        }
        // var processInfo = new ProcessStartInfo("cmd.exe", "/c " + "pdflatex main.tex")
        // {
        //   UseShellExecute = false,
        //   WorkingDirectory = folderPath,
        // };
        // var process = Process.Start(processInfo);
        // process?.WaitForExit();
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
    [Route("saveVersion/{projectId:Guid}")]
    public async Task<IActionResult> SaveVersion([FromRoute] Guid projectId, [FromBody] SaveVersionDto dto)
    {
      var version = new VersionModel
      {
        ProjectId = projectId,
        ModifiedTime = DateTime.Now
      };
      dbContext.Add(version);
      var tasks = new List<Task>();
      for (var i = 0; i < dto.Files.Count(); i++)
      {
        var newFile = new FileModel
        {
          Content = dto.Files[i].Content,
          Type = dto.Files[i].Type,
          Path = dto.Files[i].Path,
          Name = dto.Files[i].Name,
          VersionId = version.Id
        };
        await dbContext.Files.AddAsync(newFile);
      }
      dbContext.SaveChanges();
      return Ok();
    }

    [HttpGet]
    [Route("{id:Guid}")]
    public IActionResult getVersionById([FromRoute] Guid id)
    {
      var version = dbContext.Versions.Find(id);
      if (version == null) return NotFound();
      return Ok(version);
    }
  }
}