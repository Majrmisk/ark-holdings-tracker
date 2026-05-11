using System.Security.Claims;
using Application.Interfaces;
using Domain;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Services;

public class AuthService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager) : IAuthService
{
    public async Task<bool> SignInAsync(string username, string password, bool rememberMe)
    {
        var result = await signInManager.PasswordSignInAsync(username, password, rememberMe, lockoutOnFailure: false);
        return result.Succeeded;
    }

    public async Task SignOutAsync()
    {
        await signInManager.SignOutAsync();
    }

    public string? GetCurrentUserId(ClaimsPrincipal principal)
    {
        return userManager.GetUserId(principal);
    }

    public async Task<UserRole> GetCurrentUserRoleAsync(ClaimsPrincipal principal)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null) return UserRole.User;

        var roles = await userManager.GetRolesAsync(user);
        Enum.TryParse(roles.FirstOrDefault(), out UserRole role);
        return role;
    }
}
