using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using ExcelManipulator.Services;
using Microsoft.AspNetCore.Identity;

namespace ExcelManipulator.Pages.Account;

public class LoginModel : PageModel
{
    private readonly IUserService _userService;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(IUserService userService,
                      ILogger<LoginModel> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = default!;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = default!;

        public bool RememberMe { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
            return Page();

        Microsoft.AspNetCore.Identity.SignInResult result = await _userService.LoginAsync(Input.Email, Input.Password, Input.RememberMe);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in.", Input.Email);
            return LocalRedirect(ReturnUrl);
        }
        if (result.IsLockedOut)
        {
            _logger.LogWarning("User account locked out: {Email}", Input.Email);
            ModelState.AddModelError(string.Empty, "Account locked out.");
            return Page();
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return Page();
    }
}
