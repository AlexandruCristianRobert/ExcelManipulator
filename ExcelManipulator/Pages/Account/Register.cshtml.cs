using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using ExcelManipulator.Services;          // ◄─ your service
using Microsoft.AspNetCore.Identity;      // for IdentityResult

namespace ExcelManipulator.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly IUserService _userService;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(IUserService userService,
                         ILogger<RegisterModel> logger)
    {
        _userService = userService;
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

    public void OnGet(string? returnUrl = null) => ReturnUrl = returnUrl ?? Url.Content("~/");

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
            return Page();

        // 1) create the account (service seeds roles/claims internally)
        IdentityResult result = await _userService.RegisterAsync(
                                    Input.Email,
                                    Input.Password,
                                    makeAdmin: true);

        if (result.Succeeded)
        {
            // 2) sign the user in right away
            await _userService.LoginAsync(Input.Email, Input.Password, rememberMe: false);
            TempData["Status"] = "Registration successful, you are now signed in!";
            _logger.LogInformation("New user {Email} registered and signed in.", Input.Email);

            return LocalRedirect(ReturnUrl);
        }

        // 3) surface Identity errors back to the form
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return Page();
    }
}
