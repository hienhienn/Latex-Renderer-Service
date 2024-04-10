using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LatexRendererAPI.Data;
using LatexRendererAPI.Models.Domain;
using LatexRendererAPI.Models.DTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LatexRendererAPI.Controllers
{
  [Route("[controller]")]
  [ApiController]
  public class UserController : ControllerBase
  {
    private readonly AppDbContext dbContext;
    private readonly IPasswordHasher<string> passwordHasher;
    private readonly IConfiguration config;
    public UserController(AppDbContext _dbContext, IPasswordHasher<string> _passwordHasher, IConfiguration _config)
    {
      dbContext = _dbContext;
      passwordHasher = _passwordHasher;
      config = _config;
    }

    private string GenerateJSONWebToken(UserModel userInfo)
    {
      var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]));
      var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

      var claims = new[] {
        new Claim("UserId", userInfo.Id.ToString())
      };

      var token = new JwtSecurityToken(
        config["Jwt:Issuer"],
        config["Jwt:Issuer"],
        claims,
        expires: DateTime.Now.AddDays(14),
        signingCredentials: credentials);

      return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [HttpGet]
    [Route("{id:Guid}")]
    public IActionResult GetUserById([FromRoute] Guid id)
    {
      var user = dbContext.Users.Find(id);
      if (user == null)
      {
        return NotFound();
      }
      return Ok(user);
    }

    [HttpPost]
    [Route("signup")]
    public IActionResult SignUp([FromBody] AddUserRequestDto addUserRequestDto)
    {
      if (ModelState.IsValid)
      {
        var check = dbContext.Users.FirstOrDefault(x => x.Username == addUserRequestDto.Username);
        if (check != null) return BadRequest(new { title = "Username has been registered" });
        var hashedPassword = passwordHasher.HashPassword("", addUserRequestDto.Password);
        var newUser = new UserModel
        {
          Username = addUserRequestDto.Username,
          Password = hashedPassword,
          Fullname = addUserRequestDto.Fullname,
        };
        dbContext.Users.Add(newUser);
        dbContext.SaveChanges();

        var userGenJWT = new UserModel
        {
          Id = newUser.Id,
          Fullname = newUser.Fullname,
          Username = newUser.Username
        };
        var tokenString = GenerateJSONWebToken(userGenJWT);
        return CreatedAtAction(
          nameof(GetUserById),
          new { id = newUser.Id },
          new { user = newUser, token = tokenString }
        );
      }
      return BadRequest(ModelState);
    }

    [HttpPost]
    [Route("login")]
    public IActionResult Login([FromBody] LoginRequestDto loginRequestDto)
    {
      if (ModelState.IsValid)
      {
        var checkUser = dbContext.Users.FirstOrDefault(x => x.Username == loginRequestDto.Username);
        if (checkUser == null)
        {
          Console.WriteLine(1);
          return BadRequest(new { title = "Username or password is not correct!" });
        }
        var checkPasswordResult = passwordHasher.VerifyHashedPassword("", checkUser.Password, loginRequestDto.Password);
        if (checkPasswordResult == PasswordVerificationResult.Success)
        {
          var userGenJWT = new UserModel
          {
            Id = checkUser.Id,
            Fullname = checkUser.Fullname,
            Username = checkUser.Username
          };
          var tokenString = GenerateJSONWebToken(userGenJWT);
          var response = Ok(new
          {
            token = tokenString,
            user = userGenJWT,
          }
          );
          return response;
        }
        return BadRequest(new { title = "Username or password is not correct!" });
      }
      else return BadRequest(ModelState);
    }
  }

}