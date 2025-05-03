using ExcelManipulator.Data;
using ExcelManipulator.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UserService> _logger;

    private const string SeedAdminEmail = "alexandru.cristian.robert@gmail.com";

    public UserService(UserManager<User> userManager,
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

        _logger.LogInformation("User {Id} registered (Admin? {Admin})", user.Id, makeAdmin);
        return result;
    }

    public async Task<SignInResult> LoginAsync(string email, string password, bool rememberMe)
    {
        var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: true);
        _logger.LogInformation("Login attempt {Email} => {Ok}", email, result.Succeeded);
        return result;
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }


    public async Task<IdentityResult> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });

        if (string.Equals(user.Email, SeedAdminEmail, StringComparison.OrdinalIgnoreCase))
            return IdentityResult.Failed(new IdentityError { Description = "Seed admin cannot be deleted." });

        return await _userManager.DeleteAsync(user);
    }

    public async Task<IdentityResult> UpdateUserRolesAsync(string userId, IEnumerable<string> roles)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });

        var validRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        var desiredRoles = roles.Intersect(validRoles).ToList();

        var currentRoles = await _userManager.GetRolesAsync(user);
        var toRemove = currentRoles.Except(desiredRoles);
        var toAdd = desiredRoles.Except(currentRoles);

        var removeResult = await _userManager.RemoveFromRolesAsync(user, toRemove);
        if (!removeResult.Succeeded) return removeResult;

        var addResult = await _userManager.AddToRolesAsync(user, toAdd);
        return addResult;
    }

    public async Task<List<string>> GetAllRolesAsync() => await _roleManager.Roles.Select(r => r.Name!).ToListAsync();

    public async Task<List<IUserService.UserWithRoles>> GetUsersAsync(bool excludeSeedAdmin = true)
    {
        var query = _userManager.Users.AsQueryable();
        if (excludeSeedAdmin)
            query = query.Where(u => u.Email != SeedAdminEmail);

        var list = new List<IUserService.UserWithRoles>();
        foreach (var u in query.ToList())
        {
            var roles = await _userManager.GetRolesAsync(u);
            list.Add(new IUserService.UserWithRoles(u.Id, u.Email ?? string.Empty, roles));
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
                throw new InvalidOperationException(string.Join(';', create.Errors.Select(e => e.Description)));
        }

        var claims = await _roleManager.GetClaimsAsync(role);
        if (!claims.Any(c => c.Type == claimType && c.Value == claimValue))
        {
            var add = await _roleManager.AddClaimAsync(role, new Claim(claimType, claimValue));
            if (!add.Succeeded)
                throw new InvalidOperationException(string.Join(';', add.Errors.Select(e => e.Description)));
        }
    }
}
