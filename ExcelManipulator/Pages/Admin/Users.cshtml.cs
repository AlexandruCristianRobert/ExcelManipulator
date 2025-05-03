using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExcelManipulator.Services;

namespace ExcelManipulator.Pages.Admin;

[Authorize(Roles = "Admin")]
public class UsersModel : PageModel
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersModel> _logger;

    public UsersModel(IUserService userService,
                      ILogger<UsersModel> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public List<IUserService.UserWithRoles> Users { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Users = await _userService.GetUsersAsync(excludeSeedAdmin: true);
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var result = await _userService.DeleteUserAsync(id);
        TempData["Status"] = result.Succeeded ? "User deleted." : string.Join("; ", result.Errors.Select(e => e.Description));
        return RedirectToPage();
    }
}
