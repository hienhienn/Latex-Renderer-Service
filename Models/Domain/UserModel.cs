namespace LatexRendererAPI.Models.Domain
{
  public class UserModel
  {
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public string? Password { get; set; }
    public required string Fullname { get; set; }
  }
}