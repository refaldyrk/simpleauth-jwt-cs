using System.Security.Claims;
using System.Text;
using AuthSimpleAPI.Models;
using AuthSimpleAPI.Repository;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.Tokens;

namespace AuthSimpleAPI.Controllers
{ 
    [Route("api/[controller]")]
    [ApiController]
public class AuthController : ControllerBase
{
    private readonly  IConfiguration _configuration;
    private readonly  UserRepository _userRepository;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
        _userRepository = new UserRepository(configuration.GetConnectionString("MongoDBConnection"), "UserAuth");
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] UserModel userModel)
    {
        if (_userRepository.GetUserByUsername(userModel.Username) != null)
        {
            return BadRequest(new {Message = "Username already exists"});
        }

        userModel.Password = BCrypt.Net.BCrypt.HashPassword(userModel.Password);

        _userRepository.InsertUser(userModel);
        return Ok(new { Message = "Registration successfully" });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] UserModel userModel)
    {
        var user = _userRepository.GetUserByUsername(userModel.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(userModel.Password, user.Password))
        {
            return BadRequest(new { Message = "Invalid Username Or Password" });
        }

        var token = GenerateToken(user);

        return Ok(new { Token = token });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        try
        {
            return Ok(new { Message = HttpContext.Items["User"] });
        }
        catch (Exception e)
        {
            return Unauthorized(new { Message = e });
        }
    }

    private string GenerateToken(UserModel userModel)
    {
        var claims = new[]
        {
            new Claim("sub", userModel.Id)
        };

        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpireMinutes"]));

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
}
