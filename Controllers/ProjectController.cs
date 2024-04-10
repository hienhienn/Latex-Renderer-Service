using Microsoft.AspNetCore.Mvc;
// using LatexRendererAPI.Services;
using LatexRendererAPI.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using LatexRendererAPI.Models.DTO;
using LatexRendererAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace LatexRendererAPI.Controllers
{
  [Authorize]
  [Route("[controller]")]
  [ApiController]
  public class ProjectController : ControllerBase
  {
    // private IFilesService filesService;
    private AppDbContext dbContext;
    public ProjectController(AppDbContext _dbContext)
    {
      // filesService = _filesService;
      dbContext = _dbContext;
    }

    private bool CheckPermission(Guid projectId)
    {
      var userId = User.Claims.First(claim => claim.Type == "UserId").Value;
      var project = dbContext.Projects.First(x => x.Id == projectId && x.OwnerId.ToString() == userId);
      if (project != null) return true;
      return false;
    }

    // [HttpPost]
    // public IActionResult CompilePDF()
    // {
    //   var processInfo = new ProcessStartInfo("cmd.exe", "/c " + "pdflatex main.tex")
    //   {
    //     UseShellExecute = false,
    //     WorkingDirectory = Directory.GetCurrentDirectory() + "\\test",
    //   };
    //   var process = Process.Start(processInfo);
    //   process?.WaitForExit();
    //   string localFilePath = Directory.GetCurrentDirectory() + "\\test";
    //   // var response = new HttpResponseMessage(HttpStatusCode.OK);
    //   // response.Content = new StreamContent(new FileStream(localFilePath + "\\main.pdf", FileMode.Open, FileAccess.Read));
    //   // response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
    //   // response.Content.Headers.ContentDisposition.FileName = "main.pdf";
    //   // response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
    //   // string imageUrl = _urlHelper.Content("~/test/main.pdf");
    //   // return Ok(imageUrl);
    //   return Ok();
    // }

    [HttpGet]
    [Route("/files/{projectId:Guid}")]
    public IActionResult GetFilesOfProject([FromRoute] Guid projectId)
    {
      if (CheckPermission(projectId))
      {
        // var results = filesService.getFiles("projects\\" + projectId.ToString());
        return Ok();
      }
      // var results = filesService.getFiles("test");
      return Unauthorized();
    }

    [HttpGet]
    [Route("{id:Guid}")]
    public IActionResult GetProjectById([FromRoute] Guid id)
    {
      var project = dbContext.Projects.Find(id);
      if (project == null)
      {
        return NotFound();
      }
      return Ok(project);
    }

    [HttpGet]
    public IActionResult GetListProjects([FromQuery] GetListProjectsQueryDto query)
    {
      if (ModelState.IsValid)
      {
        var projects = dbContext.Projects.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
          projects = projects.Where(e => e.Name.Contains(query.Keyword));

        if (query.FieldSort != null && query.Sort != null)
        {
          if (query.FieldSort == "name")
            if (query.Sort.Equals("ascend")) projects = projects.OrderBy(x => x.Name);
            else projects = projects.OrderByDescending(x => x.Name);
          if (query.FieldSort == "lastModified")
            if (query.Sort.Equals("ascend")) projects = projects.OrderBy(x => x.LastModified);
            else projects = projects.OrderByDescending(x => x.LastModified);
        }

        var skipResults = (query.Page - 1) * query.PageSize;
        return Ok(new
        {
          list =
            projects
              .Skip(skipResults)
              .Take(query.PageSize)
              .Include(o => o.Owner)
              .ToList(),
          total = projects.Count(),
        });
      }
      return BadRequest(ModelState);
    }

    [HttpPost]
    public IActionResult CreateProject([FromBody] CreateProjectRequestDto createProjectRequestDto)
    {
      var currentUser = HttpContext.User;
      var userId = User.Claims.First(claim => claim.Type == "UserId").Value;
      var newProject = new ProjectModel
      {
        Name = createProjectRequestDto.Name,
        LastModified = DateTime.Now,
        OwnerId = Guid.Parse(userId),
        Owner = dbContext.Users.Find(Guid.Parse(userId))
      };
      dbContext.Projects.Add(newProject);
      dbContext.SaveChanges();
      var projectsPath = Directory.GetCurrentDirectory() + "\\projects\\" + newProject.Id.ToString();
      Directory.CreateDirectory(projectsPath);
      return CreatedAtAction(nameof(GetProjectById), new { id = newProject.Id }, newProject);
    }

    [HttpDelete]
    [Route("{id:Guid}")]
    public IActionResult DeleteProject([FromRoute] Guid id)
    {
      var project = dbContext.Projects.Find(id);
      if (project == null)
      {
        return NotFound();
      }
      dbContext.Projects.Remove(project);
      dbContext.SaveChanges();
      return Ok();
    }
  }

}