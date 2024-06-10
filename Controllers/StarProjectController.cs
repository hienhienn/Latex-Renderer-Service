using LatexRendererAPI.Data;
using LatexRendererAPI.Models.Domain;
using LatexRendererAPI.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LatexRendererAPI.Controllers
{
  [Authorize]
  [Route("[controller]")]
  [ApiController]
  public class StarProjectController : ControllerBase 
  {
    private AppDbContext dbContext;

    public StarProjectController(AppDbContext _dbContext)
    {
      dbContext = _dbContext;
    }

    [HttpPost] 
    public IActionResult AddStarProject([FromBody] AddUserProjectDto dto)
    {
        var project = dbContext.Projects.Find(dto.ProjectId);
        if (project == null) return NotFound();

        var user = dbContext.Users.Find(dto.EditorId);
        if (user == null) return NotFound();

        var checkExist = dbContext.StarProjects.FirstOrDefault(v => v.ProjectId == dto.ProjectId && v.EditorId == dto.EditorId);
        if(checkExist != null) return BadRequest();

        var starProject = new StarProject 
        {
            EditorId = dto.EditorId,
            ProjectId = dto.ProjectId,
        };
        dbContext.StarProjects.Add(starProject);
        dbContext.SaveChanges();
        var totalStar = dbContext.StarProjects.Where(sp => sp.ProjectId == project.Id).Count();
        return Ok(new 
        {
            totalStar,
            starProject.Id,
        });
    }


    // [HttpGet]
    // [Route("{id:Guid}")]
    // public IActionResult GetUserProjectById([FromRoute] Guid id)
    // {
    //     var userProject = dbContext.UserProjects.Find(id);
    //     if(userProject == null) return NotFound();

    //     return Ok(new {
    //         userProject.Id,
    //         userProject.EditorId,
    //         userProject.ProjectId
    //     });
    // }

    // [HttpGet]
    // [Route("userAdd/{projectId:Guid}")]
    // public IActionResult GetUserToProject([FromRoute] Guid projectId, [FromQuery] string name = "") 
    // {
    //   var project = dbContext.Projects.Find(projectId);
    //   if(project == null) return NotFound();

    //   var members = dbContext.UserProjects.Where(u => u.ProjectId == projectId);

    //   var users = dbContext.Users
    //                         .Where(
    //                           u => members.First(m => m.EditorId == u.Id) == null && 
    //                           (u.Username.ToLower().Contains(name) || u.Fullname.ToLower().Contains(name))
    //                         );
    //   return Ok(users);
    // }

    // [HttpPut]
    // public IActionResult UpdateMemberProject([FromBody] UpdateUserProjectDto dto) 
    // {
    //     var checkExist = dbContext.UserProjects.Find(dto.Id);
    //     if(checkExist == null) return NotFound();

    //     checkExist.Role = dto.Role;
    //     dbContext.SaveChanges();

    //     var users = dbContext.UserProjects
    //                         .Where(up => up.ProjectId == checkExist.ProjectId)
    //                         .Select(r => new 
    //                         {
    //                           r.Editor.Fullname,
    //                           r.Editor.Username,
    //                           r.Role,
    //                           r.EditorId,
    //                           r.Id
    //                         });
    //     return Ok(users);
    // }

    [HttpDelete]
    [Route("{id:Guid}")]
    public IActionResult DeleteStarProject([FromRoute] Guid id) 
    {
        var checkExist = dbContext.StarProjects.Find(id);
        if(checkExist == null) return NotFound();

        dbContext.StarProjects.Remove(checkExist);
        dbContext.SaveChanges();

        var totalStar = dbContext.StarProjects.Where(sp => sp.ProjectId == checkExist.ProjectId).Count();

        return Ok(totalStar);
    }
  }
}