using Application.Common;
using Application.DTOs;
using Domain;

namespace Application.Interfaces;

public interface IUserService
{
    Task<Result<IReadOnlyList<UserDto>>> GetAllUsersAsync();
    Task<Result<UserDto>> GetUserByIdAsync(string userId);
    Task<Result> CreateUserAsync(string username, string password, UserRole role);
    Task<Result> DeleteUserAsync(string userId, string callerUserId);
    Task<Result> UpdateRoleAsync(string userId, UserRole newRole, UserRole callerRole);
    Task<Result> ChangePasswordAsync(string userId, string newPassword, UserRole callerRole);
}
