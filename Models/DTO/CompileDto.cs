using System.ComponentModel.DataAnnotations;

namespace LatexRendererAPI.Models.DTO
{
  public class CompileDto
  {
    [Required]
    public required SaveFileVersionDto[] Files { get; set; }

    [Required]
    public required string Code { get; set; }
  }
}