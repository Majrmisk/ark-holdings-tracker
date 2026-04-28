using Application.DTOs;
using Domain;
using System.ComponentModel.DataAnnotations;

namespace App.Models.Admin;

public class UserListViewModel
{
    public IReadOnlyList<UserDto> Users { get; set; } = [];
    public UserRole CallerRole { get; set; }
}

public class CreateUserViewModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public UserRole SelectedRole { get; set; }

    public IEnumerable<UserRole> AvailableRoles { get; set; } = [];
}

public class EditRoleViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    [Required]
    public UserRole SelectedRole { get; set; }

    public IEnumerable<UserRole> AvailableRoles { get; set; } = [];
}

public class ChangePasswordViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
