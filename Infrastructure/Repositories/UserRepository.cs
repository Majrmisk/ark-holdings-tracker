using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Repositories;

public class UserRepository(UserManager<AppUser> userManager) : IUserRepository
{
    public async Task<IReadOnlyList<UserDto>> GetAllAsync()
    {
        var result = new List<UserDto>();

        foreach (var user in userManager.Users.ToList())
        {
            var roles = await userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? string.Empty;
            result.Add(new UserDto(user.Id, user.UserName ?? string.Empty, role, user.CreatedAt));
        }

        return result.AsReadOnly();
    }

    public async Task<UserDto?> GetByIdAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return null;

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;
        return new UserDto(user.Id, user.UserName ?? string.Empty, role, user.CreatedAt);
    }

    public async Task<Result> CreateAsync(string username, string password, UserRole role)
    {
        var user = new AppUser
        {
            UserName = username,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
            return Result.Failure(
                string.Join(" ", createResult.Errors.Select(e => e.Description)));

        var roleResult = await userManager.AddToRoleAsync(user, role.ToString());
        if (!roleResult.Succeeded)
            return Result.Failure(
                string.Join(" ", roleResult.Errors.Select(e => e.Description)));

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure("User not found.");

        var result = await userManager.DeleteAsync(user);
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result> UpdateRoleAsync(string userId, UserRole newRole)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure("User not found.");

        var currentRoles = await userManager.GetRolesAsync(user);
        foreach (var role in currentRoles)
            await userManager.RemoveFromRoleAsync(user, role);

        var result = await userManager.AddToRoleAsync(user, newRole.ToString());
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result> ChangePasswordAsync(string userId, string newPassword)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure("User not found.");

        await userManager.RemovePasswordAsync(user);
        var result = await userManager.AddPasswordAsync(user, newPassword);
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}
