using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Controllers;
using Presentation.Models;
using Shouldly;

namespace Tests.Presentation.Controllers;

public class AccountControllerTests
{
    private static (AccountController Controller, Mock<IAuthService> AuthServiceMock)
        CreateController()
    {
        var authServiceMock = new Mock<IAuthService>();
        var controller = new AccountController(authServiceMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return (controller, authServiceMock);
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
        var (controller, authServiceMock) = CreateController();
        var model = new LoginViewModel
        {
            Username = "admin",
            Password = "P@ssw0rd",
            RememberMe = false
        };
        authServiceMock.Setup(a => a.SignInAsync("admin", "P@ssw0rd", false))
            .ReturnsAsync(true);

        var result = await controller.Login(model);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("Index");
        redirect.ControllerName.ShouldBe("Home");
    }

    [Fact]
    public async Task Login_Post_ReturnsViewWithError_WhenCredentialsInvalid()
    {
        var (controller, authServiceMock) = CreateController();
        var model = new LoginViewModel
        {
            Username = "admin",
            Password = "wrong",
            RememberMe = false
        };
        authServiceMock.Setup(a => a.SignInAsync("admin", "wrong", false))
            .ReturnsAsync(false);

        var result = await controller.Login(model);

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState.ErrorCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Login_Post_ReturnsView_WhenModelStateInvalid()
    {
        var (controller, authServiceMock) = CreateController();
        controller.ModelState.AddModelError("Username", "Required");
        var model = new LoginViewModel();

        var result = await controller.Login(model);

        result.ShouldBeOfType<ViewResult>();
        authServiceMock.Verify(
            a => a.SignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task Logout_RedirectsToHome()
    {
        var (controller, authServiceMock) = CreateController();

        var result = await controller.Logout();

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("Index");
        redirect.ControllerName.ShouldBe("Home");
        authServiceMock.Verify(a => a.SignOutAsync(), Times.Once);
    }
}
