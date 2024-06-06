namespace LatexRendererAPI.Models.DTO
{
  public class UpdateUserProjectDto
  {
    public required Guid Id { get; set; }
    public required string Role { get; set; }

  }
}