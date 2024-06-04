using Microsoft.AspNetCore.Mvc;
using LatexRendererAPI.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using LatexRendererAPI.Models.DTO;
using LatexRendererAPI.Data;
using Microsoft.EntityFrameworkCore;
using LatexRendererAPI.Services;

namespace LatexRendererAPI.Controllers
{
  [Authorize]
  [Route("[controller]")]
  [ApiController]
  public class ProjectController : ControllerBase
  {
    private AppDbContext dbContext;
    private IConfiguration config;
    private IFileService fileService;
    public ProjectController(AppDbContext _dbContext, IConfiguration _config, IFileService _fileService)
    {
      dbContext = _dbContext;
      config = _config;
      fileService = _fileService;
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
          {
            DateTime now = DateTime.Now;
            if (query.Sort.Equals("ascend"))
              projects = projects
                          .Include(p => p.Versions)
                          .OrderBy(p => p.Id);
            else 
              projects = projects
                          .Include(p => p.MainVersion)
                          .OrderByDescending(p => p.MainVersion != null ? p.MainVersion.ModifiedTime : now);
          }
        }
        var skipResults = (query.Page - 1) * query.PageSize;
        return Ok(new
        {
          list =
            projects
              .Skip(skipResults)
              .Take(100)
              .Include(o => o.Owner)
              .Include(o => o.Versions)
              // .Include(o => o.MainVersion)
              .Select(p => new
              {
                p.Id,
                p.Name,
                Owner = new
                {
                  Fullname = p.Owner != null ? p.Owner.Fullname : "",
                  Username = p.Owner != null ? p.Owner.Username : "",
                },
                p.IsPublic,
                p.Versions
                // p.MainVersion
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
        // MainVersionId = new Guid()
      };
      dbContext.Projects.Add(newProject);

      var newVersion = new VersionModel
      {
        EditorId = Guid.Parse(userId),
        ProjectId = newProject.Id,
        ModifiedTime = DateTime.Now,
        IsMainVersion = true
      };
      dbContext.Versions.Add(newVersion);

      // newProject.MainVersionId = newVersion.Id;

      // var mainFile = new FileModel
      // {
      //   Name = "main.tex",
      //   Content = "",
      //   Path = "main.tex",
      //   Type = "tex",
      //   VersionId = newVersion.Id
      // };
      // dbContext.Files.Add(mainFile);

      dbContext.SaveChanges();
      // return CreatedAtAction(nameof(GetProjectById), new { id = newProject.Id }, newProject);
      return Ok();
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