using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LatexRendererAPI.Data;
using System.Diagnostics;
using LatexRendererAPI.Models.DTO;
using LatexRendererAPI.Models.Domain;

namespace LatexRendererAPI.Controllers
{
  [Authorize]
  [Route("[controller]")]
  [ApiController]
  public class VersionController : ControllerBase
  {
    private AppDbContext dbContext;
    private IConfiguration config;

    public VersionController(AppDbContext _dbContext, IConfiguration _config)
    {
      dbContext = _dbContext;
      config = _config;
    }

    [HttpPost]
    [Route("compile/{id:Guid}")]
    public async Task<IActionResult> CompilePDF([FromRoute] Guid id)
    {
      var version = dbContext.Versions.Find(id);
      if (version == null) return NotFound();
      var compilePath = Path.Combine(
          Directory.GetCurrentDirectory(),
          config["AssetPath"] ?? "",
          version.ProjectId.ToString(),
          config["CompilePath"] ?? ""
        );
      Directory.CreateDirectory(compilePath);
      var files = dbContext.Files.Where(f => f.Type == "tex" && f.IsCompile == false).ToList();
      var tasks = new List<Task>(files.Count());
      if (files.Count() > 0)
      {
        for (var i = 0; i < files.Count(); i++)
        {
          using (StreamWriter outputFile = new StreamWriter(Path.Combine(compilePath, files[i].Path)))
          {
            tasks.Add(outputFile.WriteAsync(files[i].Content));
          }
        }
        await Task.WhenAll(tasks);

        var processInfo = new ProcessStartInfo("cmd.exe", "/c " + "pdflatex main.tex")
        {
          UseShellExecute = false,
          WorkingDirectory = compilePath,
        };
        var process = Process.Start(processInfo);
        process?.WaitForExit();
      }
      return Ok($"/{version.ProjectId}/{config["CompilePath"] ?? ""}/main.pdf");
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