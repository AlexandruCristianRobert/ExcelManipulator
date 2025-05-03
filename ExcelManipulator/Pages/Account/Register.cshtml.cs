using System.ComponentModel.DataAnnotations;
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
    public class RegisterModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly SignInManager<User> _signInManager;
        private readonly IAuthenticationSchemeProvider _schemeProvider;

        public RegisterModel(IUserService userService,
                             SignInManager<User> signInManager,
                             IAuthenticationSchemeProvider schemeProvider)
        {
            _userService = userService;
            _signInManager = signInManager;
            _schemeProvider = schemeProvider;
        }

        /* ---------------------------------------------------- */
        /* View‑model                                           */
        /* ---------------------------------------------------- */
        public IList<AuthenticationScheme> ExternalLogins { get; private set; }
        public string ReturnUrl { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required, EmailAddress]
            [Display(Name = "Email address")]
            public string Email { get; set; }

            [Required, DataType(DataType.Password)]
            [StringLength(100, ErrorMessage = "{0} must be at least {2} and at most {1} characters long.", MinimumLength = 6)]
            public string Password { get; set; }

            [DataType(DataType.Password), Display(Name = "Confirm password"), Compare("Password", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; }
        }

        /* ---------------------------------------------------- */
        /* GET                                                  */
        /* ---------------------------------------------------- */
        public async Task OnGetAsync(string returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                Response.Redirect(Url.Content("~/"));
                return;
            }

            ReturnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _schemeProvider.GetAllSchemesAsync())
                              .Where(s => !string.IsNullOrEmpty(s.DisplayName) || s.Name == "Google")
                              .ToList();
        }

        /* ---------------------------------------------------- */
        /* POST                                                 */
        /* ---------------------------------------------------- */
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            if (!ModelState.IsValid)
            {
                await OnGetAsync(ReturnUrl);
                return Page();
            }

            var result = await _userService.RegisterAsync(Input.Email, Input.Password, makeAdmin: false);
            if (result.Succeeded)
            {
                var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(ReturnUrl);
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            await OnGetAsync(ReturnUrl);
            return Page();
        }
    }
}
