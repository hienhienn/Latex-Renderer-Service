using System.ComponentModel.DataAnnotations;

namespace LatexRendererAPI.Models.DTO
{
  public class SaveFileVersionDto
  {
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    [Required]
    public required string Path { get; set; }

    public required string Content { get; set; }

    [Required]
    public required string Type { get; set; }
  }
}