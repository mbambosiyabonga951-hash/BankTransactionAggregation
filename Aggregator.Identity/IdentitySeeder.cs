using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aggregator.Identity;

public class IdentitySeeder : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<IdentitySeeder> _log;

    public IdentitySeeder(IServiceProvider sp, ILogger<IdentitySeeder> log)
    {
        _sp = sp;
        _log = log;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
        // ensure refresh token table
        await db.Database.ExecuteSqlRawAsync(@"
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[auth].[RefreshTokens]') AND type in (N'U'))
            CREATE TABLE [auth].[RefreshTokens] (Id nvarchar(64) NOT NULL PRIMARY KEY, UserId nvarchar(450) NOT NULL, Token nvarchar(512) NOT NULL, ExpiresUtc datetime2 NOT NULL, CreatedUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), RevokedUtc datetime2 NULL);
            ", ct);

        var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var r in new[] { "Admin", "Analyst", "Reader" })
        {
            if (!await rm.RoleExistsAsync(r))
            {
                await rm.CreateAsync(new IdentityRole(r));
            }
        }

        var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var email = Environment.GetEnvironmentVariable("Seed__AdminEmail") ?? "admin@bankagg.local";
        var pwd = Environment.GetEnvironmentVariable("Seed__AdminPassword") ?? "Admin123!";

        var user = await um.FindByEmailAsync(email);

        if(user is null)
        {
            user = new ApplicationUser{UserName=email,Email=email,EmailConfirmed=true};
            var res = await um.CreateAsync(user, pwd);
            if (res.Succeeded)
            {
                await um.AddToRoleAsync(user, "Admin");
                await um.AddClaimsAsync(user, new[] { new System.Security.Claims.Claim("scope", "api.read"), new System.Security.Claims.Claim("scope", "api.write") });
                _log.LogInformation("Seeded admin {email}", email);
            }
            else
            {
                _log.LogError("Admin seed failed: {err}", string.Join(';', res.Errors.Select(e => e.Description)));
            }
        }
    }
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
