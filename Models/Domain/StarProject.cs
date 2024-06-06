using System.ComponentModel.DataAnnotations.Schema;

namespace LatexRendererAPI.Models.Domain
{
  public class StarProject
  {
    public Guid Id { get; set; }

    public Guid EditorId { get; set; }

    [ForeignKey("EditorId")]
    public UserModel Editor { get; set; } = null!;

    public Guid ProjectId { get; set; }

    [ForeignKey("ProjectId")]
    public ProjectModel Project { get; set; } = null!;
  }
}