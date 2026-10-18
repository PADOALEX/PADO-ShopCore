using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShopAPI.Core.Entities;
using ShopAPI.Core.Exceptions;
using ShopAPI.Infrastructure;
using ShopAPI.Web.Contracts;
using ShopAPI.Web.Infrastructure;

namespace ShopAPI.Web.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ShopDbContext db, IConfiguration config) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            throw ShopException.BadRequest("Name fields cannot be blank.");
        if (request.Password.Length < 12 || Encoding.UTF8.GetByteCount(request.Password) > 72)
            throw ShopException.BadRequest("Password must contain at least 12 characters and at most 72 UTF-8 bytes.");
        if (await db.Users.AnyAsync(x => x.Email == email, ct)) throw ShopException.Conflict("Email is already registered.");
        var user = new User { FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Email = email,
            Role = Role.User, PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12) };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Me), new { }, UserResponse.From(user));
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Problem(statusCode: 401, title: "Invalid credentials");
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email), new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var jwt = new JwtSecurityToken(issuer: config["Jwt:Issuer"], audience: config["Jwt:Audience"], claims: claims,
            notBefore: DateTime.UtcNow, expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(jwt), tokenType = "Bearer",
            expiresIn = 3600, userId = user.Id, role = user.Role.ToString() });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken ct)
    {
        var user = await db.Users.FindAsync([User.UserId()], ct)
            ?? throw ShopException.NotFound("User not found.");
        return Ok(UserResponse.From(user));
    }
}
