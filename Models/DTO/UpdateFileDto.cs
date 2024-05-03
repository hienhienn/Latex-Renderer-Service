using System.ComponentModel.DataAnnotations;

namespace LatexRendererAPI.Models.DTO
{
  public class UpdateFileDto
  {
    [MaxLength(200)]
    public string? Name { get; set; }

    public string? Path { get; set; }

    public string? Content { get; set; }

  }
}