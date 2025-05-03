using ExcelManipulator.Data;
using ExcelManipulator.Services;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UserService> _logger;

    private const string SeedAdminEmail = "alexandru.cristian.robert@gmail.com";

    public UserService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<UserService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<IdentityResult> RegisterAsync(string email, string password, bool makeAdmin = true)
    {
        await EnsureRoleWithClaimAsync("Admin", "permission", "admin-creation");
        await EnsureRoleWithClaimAsync("ExcelWorker", "permission", "excel");

        var user = new User { UserName = email, Email = email };
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded) return result;

        if (makeAdmin)
            await _userManager.AddToRoleAsync(user, "Admin");

        _logger.LogInformation("User {UserId} registered (Admin? {Admin})", user.Id, makeAdmin);
        return result;
    }

    public async Task<SignInResult> LoginAsync(string email, string password, bool rememberMe)
    {
        var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: true);
        _logger.LogInformation("Login attempt for {Email}: {Status}", email, result.Succeeded);
        return result;
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
    }

    public async Task<IdentityResult> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("DeleteUserAsync: user {Id} not found", userId);
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });
        }

        if (string.Equals(user.Email, SeedAdminEmail, StringComparison.OrdinalIgnoreCase))
            return IdentityResult.Failed(new IdentityError { Description = "Seed admin cannot be deleted." });

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
            _logger.LogInformation("User {Id} deleted.", userId);
        return result;
    }

    /// <summary>
    /// Returns all users plus their roles. Optionally excludes the seeded admin account.
    /// </summary>
    public async Task<List<IUserService.UserWithRoles>> GetUsersAsync(bool excludeSeedAdmin = true)
    {
        var query = _userManager.Users.AsQueryable();
        if (excludeSeedAdmin)
            query = query.Where(u => u.Email != SeedAdminEmail);

        var list = new List<IUserService.UserWithRoles>();
        foreach (var user in query.ToList())
        {
            var roles = await _userManager.GetRolesAsync(user);
            list.Add(new IUserService.UserWithRoles(user.Id, user.Email ?? string.Empty, roles));
        }
        return list;
    }

    private async Task EnsureRoleWithClaimAsync(string roleName, string claimType, string claimValue)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            role = new IdentityRole(roleName);
            var create = await _roleManager.CreateAsync(role);
            if (!create.Succeeded)
                throw new InvalidOperationException($"Failed creating role {roleName}: {string.Join(',', create.Errors.Select(e => e.Description))}");
        }

        var claims = await _roleManager.GetClaimsAsync(role);
        if (!claims.Any(c => c.Type == claimType && c.Value == claimValue))
        {
            var add = await _roleManager.AddClaimAsync(role, new Claim(claimType, claimValue));
            if (!add.Succeeded)
                throw new InvalidOperationException($"Failed adding claim {claimValue} to role {roleName}: {string.Join(',', add.Errors.Select(e => e.Description))}");
        }
    }
}
