namespace LatexRendererAPI.Models.DTO
{
  public class FileDto
  {
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Path { get; set; }
    public required string Content { get; set; }

  }
}