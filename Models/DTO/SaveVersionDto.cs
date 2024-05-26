using System.ComponentModel.DataAnnotations;

namespace LatexRendererAPI.Models.DTO
{
  public class SaveVersionDto
  {
    [Required]
    public required SaveFileVersionDto[] Files { get; set; }
    public string? Description { get; set; }
  }
}