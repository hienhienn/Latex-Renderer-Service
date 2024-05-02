using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LatexRendererAPI.Data;
using System.Diagnostics;
using LatexRendererAPI.Models.DTO;

namespace LatexRendererAPI.Controllers
{
  [Authorize]
  [Route("[controller]")]
  [ApiController]
  public class VersionController : ControllerBase
  {
    private AppDbContext dbContext;
    public VersionController(AppDbContext _dbContext)
    {
      dbContext = _dbContext;
    }

    [HttpPost]
    [Route("compile/{id:Guid}")]
    public async Task<IActionResult> CompilePDF([FromRoute] Guid id, [FromBody] CompileProjectDto dto)
    {
      var version = dbContext.Versions.Find(id);
      if (version == null) return NotFound();
      var versionPath = Path.Combine(
          Directory.GetCurrentDirectory(),
          "projects",
          version.ProjectId.ToString(),
          version.Id.ToString()
        );
      Directory.CreateDirectory(versionPath);
      foreach (FileDto f in dto.UpdateFiles ?? [])
      {
        using (StreamWriter outputFile = new StreamWriter(Path.Combine(versionPath, f.Path)))
        {
          await outputFile.WriteAsync(f.Content);
        }
      }
      var processInfo = new ProcessStartInfo("cmd.exe", "/c " + "pdflatex main.tex")
      {
        UseShellExecute = false,
        WorkingDirectory = versionPath,
      };
      var process = Process.Start(processInfo);
      process?.WaitForExit();
      return Ok($"/{version.ProjectId}/{version.Id}/main.pdf");
      // string localFilePath = Directory.GetCurrentDirectory() + "\\test";
      // var response = new HttpResponseMessage(HttpStatusCode.OK);
      // response.Content = new StreamContent(new FileStream(localFilePath + "\\main.pdf", FileMode.Open, FileAccess.Read));
      // response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
      // response.Content.Headers.ContentDisposition.FileName = "main.pdf";
      // response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
      // string imageUrl = _urlHelper.Content("~/test/main.pdf");
      // return Ok(imageUrl);
      // return Ok();
    }
  }
}