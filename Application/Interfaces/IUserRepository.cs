using Application.Common;
using Application.DTOs;
using Domain;

namespace Application.Interfaces;

public interface IUserRepository
{
    Task<IReadOnlyList<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(string userId);
    Task<Result> CreateAsync(string username, string password, UserRole role);
    Task<Result> DeleteAsync(string userId);
    Task<Result> UpdateRoleAsync(string userId, UserRole newRole);
    Task<Result> ChangePasswordAsync(string userId, string newPassword);
}
