using Microsoft.AspNetCore.Identity;

namespace ExcelManipulator.Services
{
    public interface IUserService
    {
        Task<IdentityResult> RegisterAsync(string email, string password, bool makeAdmin = true);
        Task<SignInResult> LoginAsync(string email, string password, bool rememberMe);
        Task LogoutAsync();

        Task<IdentityResult> DeleteUserAsync(string userId);
        Task<IdentityResult> UpdateUserRolesAsync(string userId, IEnumerable<string> roles);
        Task<List<string>> GetAllRolesAsync();
        Task<List<UserWithRoles>> GetUsersAsync(bool excludeSeedAdmin = true);

        public record UserWithRoles(string Id, string Email, IList<string> Roles);
    }
}
