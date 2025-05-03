using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ExcelManipulator.Data;

public static class IdentitySeeder
{
    private const string AdminEmail = "alexandru.cristian.robert@gmail.com";
    private const string AdminPassword = "Regele123.";

    /// <summary>Ensures roles exist and inserts a default Admin user.</summary>
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                                           .CreateLogger("IdentitySeeder");

        await EnsureRoleWithClaimAsync(roleMgr, "Admin", "permission", "admin-creation");
        await EnsureRoleWithClaimAsync(roleMgr, "ExcelWorker", "permission", "excel");

        var admin = await userMgr.FindByEmailAsync(AdminEmail);
        if (admin == null)
        {
            admin = new User { UserName = AdminEmail, Email = AdminEmail, EmailConfirmed = true };
            var result = await userMgr.CreateAsync(admin, AdminPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to create seed admin user: {Errors}",
                                string.Join(";", result.Errors.Select(e => e.Description)));
                return;
            }
            logger.LogInformation("Seed admin user created.");
        }

        // 3) ensure user is in Admin role
        if (!await userMgr.IsInRoleAsync(admin, "Admin"))
        {
            await userMgr.AddToRoleAsync(admin, "Admin");
            logger.LogInformation("Seed admin user added to Admin role.");
        }
    }

    private static async Task EnsureRoleWithClaimAsync(RoleManager<IdentityRole> roleMgr,
                                                   string roleName,
                                                   string claimType,
                                                   string claimValue)
    {
        // 1) make sure the role exists
        var role = await roleMgr.FindByNameAsync(roleName);
        if (role == null)
        {
            role = new IdentityRole(roleName);
            var createResult = await roleMgr.CreateAsync(role);
            if (!createResult.Succeeded)
                throw new InvalidOperationException(
                    $"Could not create role {roleName}: " +
                    string.Join(',', createResult.Errors.Select(e => e.Description)));
        }

        // 2) attach the claim if it isn't there yet
        var claims = await roleMgr.GetClaimsAsync(role);
        if (!claims.Any(c => c.Type == claimType && c.Value == claimValue))
        {
            var addResult = await roleMgr.AddClaimAsync(role, new Claim(claimType, claimValue));
            if (!addResult.Succeeded)
                throw new InvalidOperationException(
                    $"Could not add claim to role {roleName}: " +
                    string.Join(',', addResult.Errors.Select(e => e.Description)));
        }
    }
}
