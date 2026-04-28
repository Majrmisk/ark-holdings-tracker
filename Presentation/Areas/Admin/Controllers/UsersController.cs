using App.Models.Admin;
using Application.Interfaces;
using Domain;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Areas.Admin.Controllers;

public class UsersController(IUserService userService, UserManager<AppUser> userManager) : AdminController
{
    private static IEnumerable<UserRole> RolesFor(UserRole callerRole) =>
        callerRole == UserRole.SuperAdmin
            ? [UserRole.User, UserRole.PowerUser, UserRole.Admin]
            : [UserRole.User, UserRole.PowerUser];

    private async Task<UserRole> GetCallerRoleAsync()
    {
        var caller = await userManager.GetUserAsync(User);
        if (caller is null)
            return UserRole.User;

        var roles = await userManager.GetRolesAsync(caller);
        Enum.TryParse(roles.FirstOrDefault(), out UserRole role);
        return role;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await userService.GetAllUsersAsync();
        var callerRole = await GetCallerRoleAsync();

        return View(new UserListViewModel
        {
            Users = result.Value!,
            CallerRole = callerRole
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var callerRole = await GetCallerRoleAsync();
        return View(new CreateUserViewModel { AvailableRoles = RolesFor(callerRole) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        var callerRole = await GetCallerRoleAsync();
        model.AvailableRoles = RolesFor(callerRole);

        if (!ModelState.IsValid)
            return View(model);

        var result = await userService.CreateUserAsync(model.Username, model.Password, model.SelectedRole);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = $"User '{model.Username}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var callerUserId = userManager.GetUserId(User)!;
        var result = await userService.DeleteUserAsync(id, callerUserId);

        if (result.IsFailure)
            TempData["Error"] = result.Error;
        else
            TempData["Success"] = "User deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditRole(string id)
    {
        var result = await userService.GetUserByIdAsync(id);
        if (result.IsFailure)
            return NotFound();

        var user = result.Value!;
        var callerRole = await GetCallerRoleAsync();

        return View(new EditRoleViewModel
        {
            UserId = user.Id,
            UserName = user.UserName,
            SelectedRole = Enum.TryParse(user.Role, out UserRole r) ? r : UserRole.User,
            AvailableRoles = RolesFor(callerRole)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRole(EditRoleViewModel model)
    {
        var callerRole = await GetCallerRoleAsync();
        model.AvailableRoles = RolesFor(callerRole);

        if (!ModelState.IsValid)
            return View(model);

        var result = await userService.UpdateRoleAsync(model.UserId, model.SelectedRole, callerRole);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Role updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ChangePassword(string id)
    {
        var result = await userService.GetUserByIdAsync(id);
        if (result.IsFailure)
            return NotFound();

        var user = result.Value!;

        return View(new ChangePasswordViewModel
        {
            UserId = user.Id,
            UserName = user.UserName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var callerRole = await GetCallerRoleAsync();
        var result = await userService.ChangePasswordAsync(model.UserId, model.NewPassword, callerRole);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = $"Password for '{model.UserName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}
