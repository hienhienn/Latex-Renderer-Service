using System.ComponentModel.DataAnnotations.Schema;

namespace LatexRendererAPI.Models.Domain
{
  public class VersionModel
  {
    public Guid Id { get; set; }
    public Guid EditorId { get; set; }
    public required Guid ProjectId { get; set; }

    [ForeignKey("EditorId")]
    public UserModel? Editor { get; set; }

    [ForeignKey("ProjectId")]
    public ProjectModel? Project { get; set; }
    
    public DateTime? ModifiedTime { get; set; }
    public bool IsMainVersion { get; set; } = true;
    public string? ShaCode { get; set; }
    public string? Description { get; set; }
  }
}