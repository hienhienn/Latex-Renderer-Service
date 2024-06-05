namespace LatexRendererAPI.Models.DTO
{
  public class UpdateProjectDto
  {
    public string? Name { get; set; }
    public bool? IsPublic { get; set; }
    public string? PdfFile { get; set; }
  }
}