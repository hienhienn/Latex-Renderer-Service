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
  public class UserProjectController : ControllerBase 
  {
    private AppDbContext dbContext;

    public UserProjectController(AppDbContext _dbContext)
    {
      dbContext = _dbContext;
    }

    [HttpPost] 
    public IActionResult AddMemberToProject([FromBody] AddUserProjectDto dto)
    {
        var project = dbContext.Projects.Find(dto.ProjectId);
        if (project == null) return NotFound();

        var user = dbContext.Users.Find(dto.EditorId);
        if (user == null) return NotFound();

        var checkExist = dbContext.UserProjects.First(v => v.ProjectId == dto.ProjectId && v.EditorId == dto.EditorId);
        if(checkExist != null) return BadRequest();

        var memberProject = new UserProject 
        {
            EditorId = dto.EditorId,
            ProjectId = dto.ProjectId,
        };
        dbContext.UserProjects.Add(memberProject);
        dbContext.SaveChanges();

        return Ok();
    }


    [HttpGet]
    [Route("{id:Guid}")]
    public IActionResult GetUserProjectById([FromRoute] Guid id)
    {
        var userProject = dbContext.UserProjects.Find(id);
        if(userProject == null) return NotFound();

        return Ok(new {
            userProject.Id,
            userProject.EditorId,
            userProject.ProjectId
        });
    }
  }
}