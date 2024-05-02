using Microsoft.AspNetCore.Mvc;
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
    private AppDbContext dbContext;
    public ProjectController(AppDbContext _dbContext)
    {
      dbContext = _dbContext;
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
              .Select(p => new
              {
                p.Id,
                p.Name,
                p.LastModified,
                p.LastestVersionId,
                Owner = new
                {
                  Fullname = p.Owner != null ? p.Owner.Fullname : "",
                  Username = p.Owner != null ? p.Owner.Username : "",
                },
              })
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
        OwnerId = Guid.Parse(userId),
        LastModified = DateTime.Now
      };
      dbContext.Projects.Add(newProject);

      var newVersion = new VersionModel
      {
        EditorId = Guid.Parse(userId),
        ProjectId = newProject.Id,
        ModifiedTime = DateTime.Now,
      };
      dbContext.Versions.Add(newVersion);

      newProject.LastestVersionId = newVersion.Id;

      var mainFile = new FileModel
      {
        Name = "main.tex",
        Content = "",
        Path = "main.tex",
        Type = "tex",
        VersionId = newVersion.Id
      };
      dbContext.Files.Add(mainFile);

      dbContext.SaveChanges();
      var projectsPath = Directory.GetCurrentDirectory() + "\\projects\\" + newProject.Id.ToString();
      Directory.CreateDirectory(projectsPath);
      return CreatedAtAction(nameof(GetProjectById), new { id = newProject.Id }, newProject);
    }

    [HttpDelete]
    [Route("{id:Guid}")]
    public async Task<IActionResult> DeleteProject([FromRoute] Guid id)
    {
      var project = await dbContext.Projects.FindAsync(id);
      if (project == null)
      {
        return NotFound();
      }
      dbContext.Projects.Remove(project);
      await dbContext.SaveChangesAsync();
      return Ok();
    }
  }
}