using Microsoft.AspNetCore.Identity;

namespace ExcelManipulator.Services
{
    public interface IUserService
    {
        /* ─────────────── Local e‑mail / password ─────────────── */
        Task<IdentityResult> RegisterAsync(string email, string password, bool makeAdmin = true);
        Task<SignInResult> LoginAsync(string email, string password, bool rememberMe);
        Task LogoutAsync();

        /* ─────────────── Administration ─────────────── */
        Task<IdentityResult> DeleteUserAsync(string userId);
        Task<IdentityResult> UpdateUserRolesAsync(string userId, IEnumerable<string> roles);
        Task<IList<string>> GetAllRolesAsync();
        Task<IList<UserWithRoles>> GetUsersAsync(bool excludeSeedAdmin = true);

        /* ─────────────── External / Google OAuth ─────────────── */
        /// <summary>
        /// Complete a round‑trip for an external provider (e.g. Google).
        /// ‑ If the external identity already maps to a local user → sign them in.
        /// ‑ Otherwise create a new <see cref="User"/> WITHOUT assigning any role
        ///   and then sign them in.
        /// Returns the <see cref="SignInResult"/> produced by <see cref="SignInManager{TUser}.ExternalLoginSignInAsync"/>.
        /// </summary>
        Task<SignInResult> ExternalLoginAsync(ExternalLoginInfo info, bool isPersistent = false);

        /* ─────────────── DTOs ─────────────── */
        public record UserWithRoles(string Id, string Email, IList<string> Roles);
    }
}
