using System.ComponentModel.DataAnnotations;

namespace LatexRendererAPI.Models.DTO
{
  public class CreateProjectRequestDto
  {
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }
  }
}