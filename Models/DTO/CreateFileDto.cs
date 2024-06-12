using System.ComponentModel.DataAnnotations;

namespace LatexRendererAPI.Models.DTO
{
  public class CreateFileDto
  {
    [Required]
    public required Guid VersionId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    [Required]
    public required string Path { get; set; }

    public string? Content { get; set; }
  }
}