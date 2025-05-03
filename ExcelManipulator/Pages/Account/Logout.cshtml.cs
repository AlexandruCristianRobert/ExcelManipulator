using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using ExcelManipulator.Services;

namespace ExcelManipulator.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly IUserService _userService;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(IUserService userService,
                       ILogger<LogoutModel> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPost()
    {
        await _userService.LogoutAsync();
        _logger.LogInformation("User logged out.");
        return LocalRedirect("~/");
    }
}
