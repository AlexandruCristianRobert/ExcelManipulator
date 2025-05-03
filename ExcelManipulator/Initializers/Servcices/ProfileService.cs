// Infrastructure/ProfileService.cs
using Duende.IdentityModel;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using ExcelManipulator.Data;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

public class ProfileService : IProfileService
{
    private readonly UserManager<User> _userManager;

    public ProfileService(UserManager<User> userManager) => _userManager = userManager;

    public async Task GetProfileDataAsync(ProfileDataRequestContext ctx)
    {
        var user = await _userManager.GetUserAsync(ctx.Subject);

        var claims = ctx.Subject.Claims.ToList();

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(r => new Claim(JwtClaimTypes.Role, r)));

        var userClaims = await _userManager.GetClaimsAsync(user);
        claims.AddRange(userClaims.Where(c => c.Type == "permission"));

        ctx.IssuedClaims = claims;
    }

    public Task IsActiveAsync(IsActiveContext ctx)
    {
        ctx.IsActive = true;
        return Task.CompletedTask;
    }
}
