using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AuthSimpleAPI.Repository;
using Microsoft.IdentityModel.Tokens;

namespace AuthSimpleAPI.Middlewares;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly UserRepository _user;

    public JwtMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
        _user = new UserRepository(configuration.GetConnectionString("MongoDBConnection"), "UserAuth");
    }
    
    public async Task Invoke(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split().Last();
        if (token != null)
            AttachUser(context, token);
        await _next(context);
    }


    private void AttachUser(HttpContext ctx, string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero

            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = jwtToken.Claims.First(x => x.Type == "sub").Value;

            ctx.Items["User"] = _user.GetUserByID(userId);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}