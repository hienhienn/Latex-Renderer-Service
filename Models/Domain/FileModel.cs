namespace LatexRendererAPI.Models.Domain
{
  public class FileModel
  {
    public Guid Id { get; set; }
    public Guid VersionId { get; set; }
    public required string Name { get; set; }
    public required string Path { get; set; }
    public required string Type { get; set; } // ["img", "tex"]
    public required string Content { get; set; }
    public VersionModel Version { get; set; } = null!;
    public string? ShaCode { get; set; }
    public bool? IsCompile { get; set; }
  }
}