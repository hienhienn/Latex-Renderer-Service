using System.ComponentModel.DataAnnotations.Schema;

namespace LatexRendererAPI.Models.Domain
{
  public class VersionModel
  {
    public Guid Id { get; set; }
    public Guid EditorId { get; set; }
    public Guid ProjectId { get; set; }

    [ForeignKey("EditorId")]
    public UserModel Editor { get; set; } = null!;

    [ForeignKey("ProjectId")]
    public ProjectModel Project { get; set; } = null!;
    
    public required DateTime ModifiedTime { get; set; } = DateTime.Now;
    public bool IsMainVersion { get; set; }
    public required string Description { get; set; }
    public required Guid MainFileId { get; set; }
    public string? PdfFile { get; set; }
  }
}