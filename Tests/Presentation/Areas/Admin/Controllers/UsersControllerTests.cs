using Presentation.Models.Admin;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Presentation.Areas.Admin.Controllers;
using Shouldly;
using System.Security.Claims;

namespace Tests.Presentation.Areas.Admin.Controllers;

public class UsersControllerTests
{
    private const string CallerId = "caller-1";
    private const string TargetUserId = "user-1";
    private const string TargetUserName = "testuser";

    private static readonly UserDto TargetUser =
        new(TargetUserId, TargetUserName, nameof(UserRole.User), DateTime.UtcNow);

    private static (UsersController Controller, Mock<IUserService> ServiceMock,
        Mock<IAuthService> AuthServiceMock) CreateController(
        UserRole callerRole = UserRole.SuperAdmin)
    {
        var serviceMock = new Mock<IUserService>();
        var authServiceMock = new Mock<IAuthService>();

        authServiceMock.Setup(a => a.GetCurrentUserRoleAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(callerRole);
        authServiceMock.Setup(a => a.GetCurrentUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(CallerId);

        var controller = new UsersController(serviceMock.Object, authServiceMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, CallerId)], "TestAuth"))
            }
        };
        controller.TempData = new TempDataDictionary(
            controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

        return (controller, serviceMock, authServiceMock);
    }

    [Fact]
    public async Task Index_ReturnsViewWithUserList_WhenUsersExist()
    {
        var (controller, serviceMock, _) = CreateController();
        var users = new List<UserDto> { TargetUser };
        serviceMock.Setup(s => s.GetAllUsersAsync())
            .ReturnsAsync(Result.Success<IReadOnlyList<UserDto>>(users));

        var result = await controller.Index();

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<UserListViewModel>();
        model.Users.Count.ShouldBe(1);
        model.CallerRole.ShouldBe(UserRole.SuperAdmin);
    }

    [Fact]
    public async Task Create_Get_ReturnsViewWithAvailableRoles()
    {
        var (controller, _, _) = CreateController();

        var result = await controller.Create();

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<CreateUserViewModel>();
        model.AvailableRoles.ShouldContain(UserRole.User);
        model.AvailableRoles.ShouldContain(UserRole.PowerUser);
    }

    [Fact]
    public async Task Create_Post_RedirectsToIndex_WhenSuccessful()
    {
        var (controller, serviceMock, _) = CreateController();
        var model = new CreateUserViewModel
        {
            Username = "newuser",
            Password = "P@ssw0rd",
            SelectedRole = UserRole.User
        };
        serviceMock.Setup(s => s.CreateUserAsync("newuser", "P@ssw0rd", UserRole.User))
            .ReturnsAsync(Result.Success());

        var result = await controller.Create(model);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("Index");
        controller.TempData["Success"].ShouldNotBeNull();
    }

    [Fact]
    public async Task Create_Post_ReturnsView_WhenModelStateInvalid()
    {
        var (controller, _, _) = CreateController();
        controller.ModelState.AddModelError("Username", "Required");
        var model = new CreateUserViewModel();

        var result = await controller.Create(model);

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task Create_Post_ReturnsViewWithError_WhenServiceFails()
    {
        var (controller, serviceMock, _) = CreateController();
        var model = new CreateUserViewModel
        {
            Username = "newuser",
            Password = "P@ssw0rd",
            SelectedRole = UserRole.User
        };
        serviceMock.Setup(s => s.CreateUserAsync("newuser", "P@ssw0rd", UserRole.User))
            .ReturnsAsync(Result.Failure("Username already exists."));

        var result = await controller.Create(model);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        controller.ModelState.ErrorCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Delete_RedirectsToIndex_WithSuccessMessage_WhenSuccessful()
    {
        var (controller, serviceMock, _) = CreateController();
        serviceMock.Setup(s => s.DeleteUserAsync(TargetUserId, CallerId))
            .ReturnsAsync(Result.Success());

        var result = await controller.Delete(TargetUserId);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("Index");
        controller.TempData["Success"].ShouldNotBeNull();
    }

    [Fact]
    public async Task Delete_RedirectsToIndex_WithErrorMessage_WhenServiceFails()
    {
        var (controller, serviceMock, _) = CreateController();
        serviceMock.Setup(s => s.DeleteUserAsync(TargetUserId, CallerId))
            .ReturnsAsync(Result.Failure("Cannot delete."));

        var result = await controller.Delete(TargetUserId);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("Index");
        controller.TempData["Error"].ShouldNotBeNull();
    }

    [Fact]
    public async Task EditRole_Get_ReturnsViewWithEditModel_WhenUserExists()
    {
        var (controller, serviceMock, _) = CreateController();
        serviceMock.Setup(s => s.GetUserByIdAsync(TargetUserId))
            .ReturnsAsync(Result.Success(TargetUser));

        var result = await controller.EditRole(TargetUserId);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<EditRoleViewModel>();
        model.UserId.ShouldBe(TargetUserId);
        model.UserName.ShouldBe(TargetUserName);
    }

    [Fact]
    public async Task EditRole_Get_ReturnsNotFound_WhenUserDoesNotExist()
    {
        var (controller, serviceMock, _) = CreateController();
        serviceMock.Setup(s => s.GetUserByIdAsync(TargetUserId))
            .ReturnsAsync(Result.Failure<UserDto>("User not found."));

        var result = await controller.EditRole(TargetUserId);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task EditRole_Post_RedirectsToIndex_WhenSuccessful()
    {
        var (controller, serviceMock, _) = CreateController();
        var model = new EditRoleViewModel
        {
            UserId = TargetUserId,
            SelectedRole = UserRole.PowerUser
        };
        serviceMock.Setup(s => s.UpdateRoleAsync(TargetUserId, UserRole.PowerUser, UserRole.SuperAdmin))
            .ReturnsAsync(Result.Success());

        var result = await controller.EditRole(model);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("Index");
        controller.TempData["Success"].ShouldNotBeNull();
    }

    [Fact]
    public async Task EditRole_Post_ReturnsViewWithError_WhenServiceFails()
    {
        var (controller, serviceMock, _) = CreateController();
        var model = new EditRoleViewModel
        {
            UserId = TargetUserId,
            SelectedRole = UserRole.Admin
        };
        serviceMock.Setup(s => s.UpdateRoleAsync(TargetUserId, UserRole.Admin, UserRole.SuperAdmin))
            .ReturnsAsync(Result.Failure("Cannot modify."));

        var result = await controller.EditRole(model);

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState.ErrorCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ChangePassword_Get_ReturnsViewWithModel_WhenUserExists()
    {
        var (controller, serviceMock, _) = CreateController();
        serviceMock.Setup(s => s.GetUserByIdAsync(TargetUserId))
            .ReturnsAsync(Result.Success(TargetUser));

        var result = await controller.ChangePassword(TargetUserId);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<ChangePasswordViewModel>();
        model.UserId.ShouldBe(TargetUserId);
        model.UserName.ShouldBe(TargetUserName);
    }

    [Fact]
    public async Task ChangePassword_Get_ReturnsNotFound_WhenUserDoesNotExist()
    {
        var (controller, serviceMock, _) = CreateController();
        serviceMock.Setup(s => s.GetUserByIdAsync(TargetUserId))
            .ReturnsAsync(Result.Failure<UserDto>("User not found."));

        var result = await controller.ChangePassword(TargetUserId);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ChangePassword_Post_RedirectsToIndex_WhenSuccessful()
    {
        var (controller, serviceMock, _) = CreateController();
        var model = new ChangePasswordViewModel
        {
            UserId = TargetUserId,
            UserName = TargetUserName,
            NewPassword = "NewP@ss1",
            ConfirmPassword = "NewP@ss1"
        };
        serviceMock.Setup(s => s.ChangePasswordAsync(TargetUserId, "NewP@ss1", UserRole.SuperAdmin))
            .ReturnsAsync(Result.Success());

        var result = await controller.ChangePassword(model);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("Index");
        controller.TempData["Success"].ShouldNotBeNull();
    }

    [Fact]
    public async Task Create_Get_IncludesAdminRole_WhenCallerIsSuperAdmin()
    {
        var (controller, _, _) = CreateController(UserRole.SuperAdmin);

        var result = await controller.Create();

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<CreateUserViewModel>();
        model.AvailableRoles.ShouldContain(UserRole.Admin);
    }

    [Fact]
    public async Task Create_Get_ExcludesAdminRole_WhenCallerIsAdmin()
    {
        var (controller, _, _) = CreateController(UserRole.Admin);

        var result = await controller.Create();

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<CreateUserViewModel>();
        model.AvailableRoles.ShouldNotContain(UserRole.Admin);
    }
}
