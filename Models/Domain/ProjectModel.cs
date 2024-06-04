using System.ComponentModel.DataAnnotations.Schema;

namespace LatexRendererAPI.Models.Domain
{
  public class ProjectModel
  {
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public Guid OwnerId { get; set; }

    [ForeignKey("OwnerId")]
    public UserModel? Owner { get; set; }
    // public required Guid MainVersionId { get; set; }
    // public VersionModel? MainVersion { get; set; }
    public bool IsPublic { get; set; }
    public string? PdfFile { get; set; }
    public ICollection<UserProject> UserProjects { get; set; } = [];
    public ICollection<VersionModel> Versions { get; set; } = [];
  }
}