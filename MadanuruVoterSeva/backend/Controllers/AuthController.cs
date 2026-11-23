using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using MadanuruVoterSeva.Backend.DTOs;
using MadanuruVoterSeva.Backend.Interfaces;
using MadanuruVoterSeva.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

namespace MadanuruVoterSeva.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConfiguration _configuration;

    public AuthController(IDbConnectionFactory connectionFactory, IConfiguration configuration)
    {
        _connectionFactory = connectionFactory;
        _configuration = configuration;
    }

    [HttpPost("login")]
    [EnableRateLimiting("admin-login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username and password are required." });
        }

        using var connection = _connectionFactory.CreateConnection();
        var user = await connection.QuerySingleOrDefaultAsync<AppUser>(
            "SELECT * FROM AppUser WHERE Username = @Username COLLATE NOCASE AND Username = 'admin' AND Role = 'Admin' AND IsActive = 1;",
            new { Username = request.Username.Trim() });

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        // Generate JWT token
        var secret = _configuration["JwtSettings:Secret"] ?? throw new InvalidOperationException("Session signing key is unavailable.");
        var key = Encoding.UTF8.GetBytes(secret);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddHours(12),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        Response.Cookies.Append("MadanuruDesk", tokenString, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = Request.IsHttps,
            MaxAge = TimeSpan.FromHours(12)
        });

        return Ok(new LoginResponseDto
        {
            Token = tokenString,
            Username = user.Username,
            Role = user.Role
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("MadanuruDesk");
        return NoContent();
    }

    [HttpGet("me")]
    public ActionResult GetCurrentUser()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Ok(new
            {
                username = User.Identity.Name,
                role = User.FindFirst(ClaimTypes.Role)?.Value,
                isAuthenticated = true
            });
        }
        return Ok(new { isAuthenticated = false });
    }
}
