namespace LatexRendererAPI.Models.DTO
{
  public class AddUserProjectDto
  {

    public required Guid EditorId { get; set; }

    public required Guid ProjectId { get; set; }

    public string? Role { get; set; } = "viewer"; // viewer, editor

  }
}