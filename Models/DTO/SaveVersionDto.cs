using System.ComponentModel.DataAnnotations;

namespace LatexRendererAPI.Models.DTO
{
  public class SaveVersionDto
  {
    [Required]
    public required SaveFileVersionDto[] Files { get; set; }
    public required string Description { get; set; }
    public required string MainFilePath { get; set; }
  }
}