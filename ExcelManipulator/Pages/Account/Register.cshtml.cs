using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ExcelManipulator.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class RegisterModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    [BindProperty] public InputModel Input { get; set; } = default!;

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = default!;

        [Required, DataType(DataType.Password), MinLength(8)]
        public string Password { get; set; } = default!;

        [Required, DataType(DataType.Password),
         Compare(nameof(Password), ErrorMessage = "Passwords must match.")]
        public string ConfirmPassword { get; set; } = default!;
    }

    public void OnGet(string? returnUrl = null) => ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
            return Page();

        // Ensure roles exist with their claims (admin & excel‑worker)
        await EnsureRoleWithClaimAsync("Admin", new Claim("permission", "admin-creation"));
        await EnsureRoleWithClaimAsync("ExcelWorker", new Claim("permission", "excel"));

        var user = new User { UserName = Input.Email, Email = Input.Email };
        var result = await _userManager.CreateAsync(user, Input.Password);

        if (result.Succeeded)
        {
            // Make every newly‑registered user an Admin by default.
            await _userManager.AddToRoleAsync(user, "Admin");

            // Optionally sign the user in immediately.
            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("User {UserId} created and added to Admin role.", user.Id);
            return LocalRedirect(ReturnUrl);
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return Page();
    }

    private async Task EnsureRoleWithClaimAsync(string roleName, Claim claim)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            role = new IdentityRole(roleName);
            var roleResult = await _roleManager.CreateAsync(role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not create {roleName} role: {string.Join(',', roleResult.Errors.Select(e => e.Description))}");
            }
        }

        var roleClaims = await _roleManager.GetClaimsAsync(role);
        if (!roleClaims.Any(c => c.Type == claim.Type && c.Value == claim.Value))
        {
            var claimResult = await _roleManager.AddClaimAsync(role, claim);
            if (!claimResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not add claim {claim} to role {roleName}: {string.Join(',', claimResult.Errors.Select(e => e.Description))}");
            }
        }
    }
}