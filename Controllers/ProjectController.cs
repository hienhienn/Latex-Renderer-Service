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

        var currentUser = HttpContext.User;
        var userId = User.Claims.First(claim => claim.Type == "UserId").Value;

        if (query.Category == "all")
          projects = projects
          .Include(p => p.UserProjects)
          .Where(p => p.UserProjects.First(up => up.EditorId == Guid.Parse(userId)) != null);
        else if (query.Category == "yours")
          projects = projects
            .Include(p => p.UserProjects)
            .Where(p => p.UserProjects.First(up => up.EditorId == Guid.Parse(userId) && up.Role == "owner") != null);
        else if (query.Category == "shared")
          projects = projects
            .Include(p => p.UserProjects)
            .Where(p => p.UserProjects.First(up => up.EditorId == Guid.Parse(userId) && up.Role != "owner") != null);
        else if (query.Category == "starred")
          projects = projects
            .Include(p => p.StarProjects)
            .Where(p => p.StarProjects.First(up => up.EditorId == Guid.Parse(userId)) != null);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
          projects = projects.Where(e => e.Name.Contains(query.Keyword));

        if (query.FieldSort != null && query.Sort != null)
        {
          if (query.FieldSort == "name")
            if (query.Sort.Equals("ascend")) projects = projects.OrderBy(x => x.Name);
            else projects = projects.OrderByDescending(x => x.Name);
          else if (query.FieldSort == "modifiedTime")
          {
            if (query.Sort.Equals("ascend"))
              projects = projects
                          .Where(p => p.Versions != null)
                          .Include(p => p.Versions)
                          .OrderBy(p => p.Versions.First(x => x.IsMainVersion).ModifiedTime);

            else
              projects = projects
                          .Where(p => p.Versions != null)
                          .Include(p => p.Versions)
                          .OrderByDescending(p => p.Versions.First(x => x.IsMainVersion).ModifiedTime);
          }
        }
        var skipResults = (query.Page - 1) * query.PageSize;
        return Ok(new
        {
          list =
            projects
              .Skip(skipResults)
              .Take(query.PageSize)
              .Include(o => o.Versions)
              .Include(o => o.UserProjects)
              .Select(p => new
              {
                p.Id,
                p.Name,
                p.IsPublic,
                p.MainVersionId,
                UserProjects = p.UserProjects.Select(up => new
                {
                  up.Editor.Fullname,
                  up.Editor.Username,
                  up.EditorId,
                  userId,
                  up.Role,
                }),
                p.Versions.First(x => x.IsMainVersion).ModifiedTime,
                Editor = new
                {
                  p.Versions.First(x => x.IsMainVersion).Editor.Fullname,
                  p.Versions.First(x => x.IsMainVersion).Editor.Username
                }
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
        MainVersionId = new Guid()
      };
      dbContext.Projects.Add(newProject);

      var newUserProject = new UserProject
      {
        ProjectId = newProject.Id,
        EditorId = Guid.Parse(userId),
        Role = "owner"
      };
      dbContext.UserProjects.Add(newUserProject);

      var newVersion = new VersionModel
      {
        EditorId = Guid.Parse(userId),
        ProjectId = newProject.Id,
        ModifiedTime = DateTime.Now,
        IsMainVersion = true,
        MainFileId = new Guid(),
        Description = "Main Version"
      };
      dbContext.Versions.Add(newVersion);

      newProject.MainVersionId = newVersion.Id;

      var mainFile = new FileModel
      {
        Name = "main.tex",
        Content = "",
        Path = "main.tex",
        Type = "tex",
        VersionId = newVersion.Id
      };
      dbContext.Files.Add(mainFile);

      newVersion.MainFileId = mainFile.Id;

      dbContext.SaveChanges();
      // return Ok(newProject);
      return Ok();
    }

    [HttpPost]
    [Route("copyProject")]
    public IActionResult CopyProject([FromBody] CopyProjectDto dto)
    {
      var currentUser = HttpContext.User;
      var userId = User.Claims.First(claim => claim.Type == "UserId").Value;

      var project = new ProjectModel
      {
        Name = dto.Name,
        MainVersionId = new Guid(),
      };
      dbContext.Add(project);

      var version = new VersionModel
      {
        ProjectId = project.Id,
        ModifiedTime = DateTime.Now,
        EditorId = Guid.Parse(userId),
        IsMainVersion = true,
        Description = "Main version",
        MainFileId = new Guid()
      };
      dbContext.Add(version);

      project.MainVersionId = version.Id;

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
      var mainFile = dbContext.Files.First(f => f.Path == dto.MainFilePath && f.VersionId == version.Id);
      if (mainFile != null) version.MainFileId = mainFile.Id;
      dbContext.SaveChanges();
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

    [HttpPut]
    [Route("{id:Guid}")]
    public IActionResult UpdateProject([FromRoute] Guid id, [FromBody] UpdateProjectDto dto)
    {
      var project = dbContext.Projects.Find(id);
      if (project == null)
      {
        return NotFound();
      }
      if (dto.Name != null && dto.Name != "") project.Name = dto.Name;
      if (dto.IsPublic != null) project.IsPublic = dto.IsPublic;

      dbContext.SaveChanges();

      return Ok(dto);
    }
  }
}