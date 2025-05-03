using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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

    // table rows
    public List<IUserService.UserWithRoles> Users { get; private set; } = new();

    // dropdown data for the shared modal
    public List<SelectListItem> RoleSelectList { get; private set; } = new();

    // binds modal form fields
    [BindProperty]
    public UpdateRolesDto UpdateRolesInput { get; set; } = new();

    public class UpdateRolesDto
    {
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public List<string> SelectedRoles { get; set; } = new();
    }

    // -------- handlers -----------------------------------------------------------

    public async Task OnGetAsync() => await PopulateAsync();

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var result = await _userService.DeleteUserAsync(id);
        TempData["Status"] = result.Succeeded
            ? "User deleted."
            : string.Join("; ", result.Errors.Select(e => e.Description));
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateRolesAsync()
    {
        if (string.IsNullOrWhiteSpace(UpdateRolesInput.UserId))
        {
            TempData["Status"] = "Invalid user.";
            return RedirectToPage();
        }

        var result = await _userService.UpdateUserRolesAsync(
                        UpdateRolesInput.UserId!,
                        UpdateRolesInput.SelectedRoles);

        TempData["Status"] = result.Succeeded
            ? "Roles updated."
            : string.Join("; ", result.Errors.Select(e => e.Description));

        return RedirectToPage();
    }

    // -------- helpers ------------------------------------------------------------

    private async Task PopulateAsync()
    {
        Users = (List<IUserService.UserWithRoles>)await _userService.GetUsersAsync(excludeSeedAdmin: true);
        RoleSelectList = (await _userService.GetAllRolesAsync())
                         .Select(r => new SelectListItem(r, r))
                         .ToList();
    }
}
