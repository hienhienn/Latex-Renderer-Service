using System.ComponentModel.DataAnnotations;

namespace LatexRendererAPI.Models.DTO
{
  public class LoginRequestDto
  {
    [Required]
    [MinLength(2, ErrorMessage = "Username length can't be less than 2.")]
    [MaxLength(200, ErrorMessage = "Username length can't be more than 200.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Invalid characters.")]
    public required string Username { get; set; }

    [Required]
    [MinLength(6, ErrorMessage = "Password length can't be less than 6.")]
    [MaxLength(20, ErrorMessage = "Password length can't be more than 20.")]
    [RegularExpression(@"^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[a-zA-Z]).{6,20}$", ErrorMessage = "Invalid password.")]
    public string? Password { get; set; }
  }
}