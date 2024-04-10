using System.ComponentModel.DataAnnotations;

namespace LatexRendererAPI.Models.DTO
{
  public class CreateProjectRequestDto
  {
    [Required]
    public required string Name { get; set; }
  }
}