using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain;

namespace Application.Services;

public class UserService(IUserRepository userRepository) : IUserService
{
    private static readonly UserRole[] ManageableByAdmin = [UserRole.User, UserRole.PowerUser];

    public async Task<Result<IReadOnlyList<UserDto>>> GetAllUsersAsync()
    {
        var users = await userRepository.GetAllAsync();
        return Result.Success(users);
    }

    public async Task<Result<UserDto>> GetUserByIdAsync(string userId)
    {
        var user = await userRepository.GetByIdAsync(userId);
        return user is null
            ? Result.Failure<UserDto>("User not found.")
            : Result.Success(user);
    }

    public async Task<Result> CreateUserAsync(string username, string password, UserRole role)
    {
        if (role == UserRole.SuperAdmin)
            return Result.Failure("Cannot create a SuperAdmin user.");

        return await userRepository.CreateAsync(username, password, role);
    }

    public async Task<Result> DeleteUserAsync(string userId, string callerUserId)
    {
        if (userId == callerUserId)
            return Result.Failure("You cannot delete your own account.");

        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            return Result.Failure("User not found.");

        if (user.Role == nameof(UserRole.SuperAdmin))
            return Result.Failure("SuperAdmin accounts cannot be deleted.");

        return await userRepository.DeleteAsync(userId);
    }

    public async Task<Result> UpdateRoleAsync(string userId, UserRole newRole, UserRole callerRole)
    {
        if (newRole == UserRole.SuperAdmin)
            return Result.Failure("Cannot assign the SuperAdmin role.");

        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            return Result.Failure("User not found.");

        if (user.Role == nameof(UserRole.SuperAdmin))
            return Result.Failure("Cannot modify a SuperAdmin account.");

        if (callerRole == UserRole.Admin)
        {
            if (!ManageableByAdmin.Select(r => r.ToString()).Contains(user.Role))
                return Result.Failure("Admins can only manage User and PowerUser accounts.");

            if (!ManageableByAdmin.Contains(newRole))
                return Result.Failure("Admins can only assign User or PowerUser roles.");
        }

        return await userRepository.UpdateRoleAsync(userId, newRole);
    }

    public async Task<Result> ChangePasswordAsync(string userId, string newPassword, UserRole callerRole)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            return Result.Failure("User not found.");

        if (user.Role == nameof(UserRole.SuperAdmin))
            return Result.Failure("Cannot change the password of a SuperAdmin account.");

        if (callerRole == UserRole.Admin &&
            !ManageableByAdmin.Select(r => r.ToString()).Contains(user.Role))
            return Result.Failure("Admins can only manage User and PowerUser accounts.");

        return await userRepository.ChangePasswordAsync(userId, newPassword);
    }
}
