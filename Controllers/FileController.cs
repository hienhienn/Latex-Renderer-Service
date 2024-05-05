
using LatexRendererAPI.Data;
using LatexRendererAPI.Models.Domain;
using LatexRendererAPI.Models.DTO;
using LatexRendererAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LatexRendererAPI.Controllers
{
  [Authorize]
  [Route("[controller]")]
  [ApiController]
  public class FileController : ControllerBase
  {
    private AppDbContext dbContext;
    private IFileService fileService;


    public FileController(AppDbContext _dbContext, IFileService _fileService)
    {
      dbContext = _dbContext;
      fileService = _fileService;

    }

    [HttpGet]
    [Route("getAll/{versionId:Guid}")]
    public IActionResult GetFilesVersion([FromRoute] Guid versionId)
    {
      var files = dbContext.Files
      .Select(f => new
      {
        f.Id,
        f.Name,
        f.Path,
        f.VersionId,
        f.Type,
      })
      .Where(f => f.VersionId == versionId)
      .OrderBy(f => f.Path);
      return Ok(files);
    }

    [HttpGet]
    [Route("{id:Guid}")]
    public IActionResult GetFileById([FromRoute] Guid id)
    {
      var file = dbContext.Files.Find(id);
      if (file == null)
      {
        return NotFound();
      }
      return Ok(file);
    }

    [HttpPost]
    [Route("uploadFile")]
    public async Task<IActionResult> UploadImage(
      [FromForm] IFormFile file,
      [FromForm] string name,
      [FromForm] Guid versionId,
      [FromForm] string path
    )
    {
      var version = await dbContext.Versions.FirstOrDefaultAsync(p => p.Id == versionId);
      if (version == null) return NotFound();
      var projectId = version.ProjectId;
      var filePath = await fileService.SaveFile(file, name, projectId);

      var fileModel = new FileModel
      {
        Name = $"{name}{Path.GetExtension(file.FileName)}",
        Content = filePath,
        VersionId = versionId,
        Path = path,
        Type = "img",
      };
      dbContext.Add(fileModel);
      await dbContext.SaveChangesAsync();
      return CreatedAtAction(nameof(GetFileById), new { id = fileModel.Id }, fileModel);
    }

    [HttpPost]
    [Route("createFile")]
    public IActionResult CreateFile([FromBody] CreateFileDto createFileDto)
    {
      if (ModelState.IsValid)
      {
        var version = dbContext.Versions.FirstOrDefault(p => p.Id == createFileDto.VersionId);
        if (version == null) return NotFound();
        var projectId = version.ProjectId;

        var fileModel = new FileModel
        {
          Name = createFileDto.Name,
          Content = "",
          VersionId = createFileDto.VersionId,
          Path = createFileDto.Path,
          Type = "tex",
        };
        dbContext.Add(fileModel);
        dbContext.SaveChanges();
        return CreatedAtAction(nameof(GetFileById), new { id = fileModel.Id }, fileModel);
      }
      return BadRequest(ModelState);
    }

    [HttpPut]
    [Route("updateFile/{id:Guid}")]
    public IActionResult UpdateFile([FromRoute] Guid id, [FromBody] UpdateFileDto dto)
    {
      if (ModelState.IsValid)
      {
        var file = dbContext.Files.FirstOrDefault(p => p.Id == id);
        if (file == null)
        {
          return NotFound();
        }
        if (dto.Content != null) file.Content = dto.Content;
        if (dto.Name != null) file.Content = dto.Name;
        if (dto.Path != null) file.Content = dto.Path;
        dbContext.SaveChanges();

        return Ok(file.Id);
      }
      return BadRequest(ModelState);
    }

    [HttpDelete]
    [Route("deleteFile/{id:Guid}")]
    public IActionResult DeleteFile([FromRoute] Guid id)
    {

      var file = dbContext.Files.Find(id);
      if (file == null)
      {
        return NotFound();
      }
      dbContext.Files.Remove(file);
      dbContext.SaveChanges();

      return Ok();
    }
  }


}