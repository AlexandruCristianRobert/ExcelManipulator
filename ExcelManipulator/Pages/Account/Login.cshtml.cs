using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using ExcelManipulator.Data;
using ExcelManipulator.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExcelManipulator.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public class LoginModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly SignInManager<User> _signInManager;
        private readonly IAuthenticationSchemeProvider _schemeProvider;

        public LoginModel(IUserService userService,
                          SignInManager<User> signInManager,
                          IAuthenticationSchemeProvider schemeProvider)
        {
            _userService = userService;
            _signInManager = signInManager;
            _schemeProvider = schemeProvider;
        }

        public IList<AuthenticationScheme> ExternalLogins { get; private set; } = new List<AuthenticationScheme>();
        public string ReturnUrl { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required, EmailAddress]
            public string Email { get; set; }

            [Required, DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _schemeProvider.GetAllSchemesAsync())
                              .Where(s => !string.IsNullOrEmpty(s.DisplayName) || s.Name == "Google")
                              .ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            if (!ModelState.IsValid)
            {
                await OnGetAsync(ReturnUrl); // repopulate ExternalLogins
                return Page();
            }

            var result = await _userService.LoginAsync(Input.Email, Input.Password, Input.RememberMe);
            if (result.Succeeded)
                return LocalRedirect(ReturnUrl);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "This account is locked out.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            await OnGetAsync(ReturnUrl); // refresh external schemes
            return Page();
        }

     
        public IActionResult OnPostExternalLogin(string provider, string returnUrl = null)
        {
            // Build redirect back to callback handler
            var redirectUrl = Url.Page("./Login", "ExternalLoginCallback", new { returnUrl });
            var props = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, props);
        }

        public async Task<IActionResult> OnGetExternalLoginCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");

            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"External provider error: {remoteError}");
                await OnGetAsync(ReturnUrl);
                return Page();
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ModelState.AddModelError(string.Empty, "Error loading external login information.");
                await OnGetAsync(ReturnUrl);
                return Page();
            }

            var signInResult = await _userService.ExternalLoginAsync(info);

            if (signInResult.Succeeded)
                return LocalRedirect(ReturnUrl);

            if (signInResult.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Account locked.");
            }
            
            else
            {
                ModelState.AddModelError(string.Empty, "External login failed. Contact support.");
            }

            await OnGetAsync(ReturnUrl);
            return Page();
        }
    }
}