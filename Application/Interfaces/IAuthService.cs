using System.Security.Claims;
using Domain;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<bool> SignInAsync(string username, string password, bool rememberMe);
    Task SignOutAsync();
    string? GetCurrentUserId(ClaimsPrincipal principal);
    Task<UserRole> GetCurrentUserRoleAsync(ClaimsPrincipal principal);
}
