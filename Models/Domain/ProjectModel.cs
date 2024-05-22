namespace LatexRendererAPI.Models.Domain
{
  public class ProjectModel
  {
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public Guid OwnerId { get; set; }
    public UserModel? Owner { get; set; }
    public DateTime? LastModified { get; set; }
    public Guid? LastestVersionId { get; set; }

  }
}