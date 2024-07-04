using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace ChatSample.Controllers;

[ApiController]
[Route("[controller]")]
public class LoginController(IConfiguration configuration) : ControllerBase
{
    private readonly string[] _testLoginIds = ["admin", "user"];
    
    /// <summary>
    /// 로그인 후 JWT Access Token을 발급합니다.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // TODO : 토큰 발급 로직 구현
        if (request.Password == "1234" && _testLoginIds.Contains(request.Email))
        {
            var token = GenerateJwtToken(request.Email);
            return Ok(new { Token = token });
        }

        return Unauthorized();
    }
    
    private string GenerateJwtToken(string email)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"]!,
            audience: configuration["Jwt:Audience"]!,
            claims: claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}