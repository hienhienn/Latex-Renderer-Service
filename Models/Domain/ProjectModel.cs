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
    public DateTime? LastModified { get; set; }
    public Guid LastModifiedUserId { get; set; }

    [ForeignKey("LastModifiedUserId")]
    public UserModel? LastModifiedUser { get; set; }

    public Guid? LastestVersionId { get; set; }
    public bool IsPublic { get; set; }
    public ICollection<UserProject> UserProjects { get; set; } = [];
  }
}