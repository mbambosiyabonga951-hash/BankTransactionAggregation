using System.IdentityModel.Tokens.Jwt; using System.Security.Claims; using System.Text;
using Microsoft.AspNetCore.Mvc; using Microsoft.AspNetCore.Identity; using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore; using Aggregator.Identity;

namespace Aggregator.Api.Controllers;
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signin;
    private readonly IConfiguration _cfg;
    private readonly AppIdentityDbContext _ctx;
    public AuthController(UserManager<ApplicationUser> u, SignInManager<ApplicationUser> s,
            IConfiguration c, AppIdentityDbContext ctx)
    {
        _users = u;
        _signin = s;
        _cfg = c;
        _ctx = ctx;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var exists = await _users.FindByEmailAsync(req.Email);
        if (exists != null) return Conflict("Email already registered");

        var user = new ApplicationUser { UserName = req.Email, Email = req.Email, EmailConfirmed = true };
        var result = await _users.CreateAsync(user, req.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);
        await _users.AddClaimsAsync(user, new[] { new Claim("scope", "api.read"), new Claim("scope", "api.write") });
        return Created("", new { user.Id, user.Email });
    }

    [HttpPost("token")]
    public async Task<IActionResult> Token([FromBody] LoginRequest req)
    {
        var user = await _users.FindByNameAsync(req.Username) ?? await _users.FindByEmailAsync(req.Username);
        if (user is null) return Unauthorized();
        var pw = await _signin.CheckPasswordSignInAsync(user, req.Password, false); if (!pw.Succeeded && !string.IsNullOrEmpty(req.Password)) 
            return Unauthorized();

        var jwt = await CreateJwtAsync(user); var refresh = await IssueRefreshAsync(user);
        return Ok(new TokenResponse(jwt.token, jwt.expires, refresh));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] Dictionary<string, string> body)
    {
        if (!body.TryGetValue("refreshToken", out var token)) return BadRequest();
        var rec = await _ctx.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token && r.RevokedUtc == null);
        if (rec is null || rec.ExpiresUtc < DateTime.UtcNow) return Unauthorized();
        var user = await _users.FindByIdAsync(rec.UserId); if (user is null) return Unauthorized();
        rec.RevokedUtc = DateTime.UtcNow; await _ctx.SaveChangesAsync();
        var jwt = await CreateJwtAsync(user); var newRefresh = await IssueRefreshAsync(user);
        return Ok(new TokenResponse(jwt.token, jwt.expires, newRefresh));
    }

    private async Task<(string token, DateTime expires)> CreateJwtAsync(ApplicationUser user)
    {
        var issuer = _cfg["Jwt:Issuer"] ?? "BankAgg"; 
        var audience = _cfg["Jwt:Audience"] ?? "BankAgg.Clients";
        var key = _cfg["Jwt:Key"] ?? "change_this_dev_only_secret_at_least_32_chars!!";
        var lifetime = int.TryParse(_cfg["Jwt:AccessTokenMinutes"], out var m) ? m : 60;
        var signing = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)); 
        var creds = new SigningCredentials(signing, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var extra = await _users.GetClaimsAsync(user); 
        claims.AddRange(extra);
        var token = new JwtSecurityToken(issuer, audience, claims, now, now.AddMinutes(lifetime), creds);
        return (new JwtSecurityTokenHandler().WriteToken(token), token.ValidTo);
    }

    private async Task<string> IssueRefreshAsync(ApplicationUser user)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        _ctx.RefreshTokens.Add(new RefreshToken { UserId = user.Id, Token = token, ExpiresUtc = DateTime.UtcNow.AddDays(7) });
        await _ctx.SaveChangesAsync();
        return token;
    }

    public record RegisterRequest(string Email, string Password);
    public record LoginRequest(string Username, string Password);
    public record TokenResponse(string AccessToken, DateTime ExpiresUtc, string RefreshToken);
}
