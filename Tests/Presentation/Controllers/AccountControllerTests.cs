using App.Controllers;
using App.Models;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;

namespace Tests.Presentation.Controllers;

public class AccountControllerTests
{
    private static (AccountController Controller, Mock<SignInManager<AppUser>> SignInManagerMock)
        CreateController()
    {
        var userStoreMock = new Mock<IUserStore<AppUser>>();
        var userManagerMock = new Mock<UserManager<AppUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
        var signInManagerMock = new Mock<SignInManager<AppUser>>(
            userManagerMock.Object, contextAccessorMock.Object, claimsFactoryMock.Object,
            null!, null!, null!, null!);

        var controller = new AccountController(userManagerMock.Object, signInManagerMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return (controller, signInManagerMock);
    }

    [Fact]
    public void Login_Get_ReturnsView()
    {
        var (controller, _) = CreateController();

        var result = controller.Login();

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task Login_Post_RedirectsToHome_WhenCredentialsValid()
    {
        var (controller, signInManagerMock) = CreateController();
        var model = new LoginViewModel
        {
            Username = "admin",
            Password = "P@ssw0rd",
            RememberMe = false
        };
        signInManagerMock.Setup(m => m.PasswordSignInAsync("admin", "P@ssw0rd", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var result = await controller.Login(model);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("Index");
        redirect.ControllerName.ShouldBe("Home");
    }

    [Fact]
    public async Task Login_Post_ReturnsViewWithError_WhenCredentialsInvalid()
    {
        var (controller, signInManagerMock) = CreateController();
        var model = new LoginViewModel
        {
            Username = "admin",
            Password = "wrong",
            RememberMe = false
        };
        signInManagerMock.Setup(m => m.PasswordSignInAsync("admin", "wrong", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var result = await controller.Login(model);

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState.ErrorCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Login_Post_ReturnsView_WhenModelStateInvalid()
    {
        var (controller, signInManagerMock) = CreateController();
        controller.ModelState.AddModelError("Username", "Required");
        var model = new LoginViewModel();

        var result = await controller.Login(model);

        result.ShouldBeOfType<ViewResult>();
        signInManagerMock.Verify(
            m => m.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task Logout_RedirectsToHome()
    {
        var (controller, signInManagerMock) = CreateController();

        var result = await controller.Logout();

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("Index");
        redirect.ControllerName.ShouldBe("Home");
        signInManagerMock.Verify(m => m.SignOutAsync(), Times.Once);
    }
}
