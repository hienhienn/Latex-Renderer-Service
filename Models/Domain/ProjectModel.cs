using System.ComponentModel.DataAnnotations.Schema;

namespace LatexRendererAPI.Models.Domain
{
  public class ProjectModel
  {
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required Guid MainVersionId { get; set; }
    public bool IsPublic { get; set; } = false;
    public ICollection<UserProject> UserProjects { get; set; } = [];
    public ICollection<StarProject> StarProjects { get; set; } = [];
    public ICollection<VersionModel> Versions { get; set; } = [];
  }
}