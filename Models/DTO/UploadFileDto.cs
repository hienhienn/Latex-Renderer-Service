using System.ComponentModel.DataAnnotations;

namespace LatexRendererAPI.Models.DTO
{
  public class UploadFileDto
  {
    [Required]
    public required Guid VersionId;

    [Required]
    [MaxLength(200)]
    public required string Name;

    [Required]
    public required string Path;

    [Required]
    public required IFormFile File;
  }
}