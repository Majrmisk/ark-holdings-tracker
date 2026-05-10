using App.Controllers;
using App.Models;
using Application.Interfaces;
using Domain;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;
using Application.Services;
using System.Security.Claims;

namespace Tests.Presentation.Controllers;

public class TableControllerTests
{
    private static (TableController Controller, Mock<IHoldingService> ServiceMock,
        Mock<UserManager<AppUser>> UserManagerMock) CreateController(
        UserRole callerRole = UserRole.User)
    {
        var serviceMock = new Mock<IHoldingService>();
        var storeMock = new Mock<IUserStore<AppUser>>();
        var userManagerMock = new Mock<UserManager<AppUser>>(
            storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var callerUser = new AppUser { Id = "caller-1", UserName = "caller" };
        userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(callerUser);
        userManagerMock.Setup(m => m.GetRolesAsync(callerUser))
            .ReturnsAsync(new List<string> { callerRole.ToString() });

        var controller = new TableController(userManagerMock.Object, serviceMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "caller-1")], "TestAuth"))
            }
        };
        return (controller, serviceMock, userManagerMock);
    }

    [Fact]
    public async Task Index_ReturnsViewWithTableViewModel_WhenDiffAndUserExist()
    {
        var (controller, serviceMock, _) = CreateController();
        var diff = new HoldingDiff(Guid.NewGuid(), Guid.NewGuid(), []);
        serviceMock.Setup(s => s.GetLatestDiffAsync()).ReturnsAsync(diff);

        var result = await controller.Index();

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<TableViewModel>();
        model.Diff.ShouldBe(diff);
        model.ColorScheme.ShouldNotBeNull();
    }

    [Fact]
    public async Task Index_ReturnsNotFound_WhenNoDiffExists()
    {
        var (controller, serviceMock, _) = CreateController();
        serviceMock.Setup(s => s.GetLatestDiffAsync()).ReturnsAsync((HoldingDiff?)null);

        var result = await controller.Index();

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Index_ReturnsUnauthorized_WhenUserNotFound()
    {
        var (controller, serviceMock, userManagerMock) = CreateController();
        var diff = new HoldingDiff(Guid.NewGuid(), Guid.NewGuid(), []);
        serviceMock.Setup(s => s.GetLatestDiffAsync()).ReturnsAsync(diff);
        userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((AppUser?)null);

        var result = await controller.Index();

        result.ShouldBeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Index_UsesCorrectColorScheme_ForAdminRole()
    {
        var (controller, serviceMock, _) = CreateController(UserRole.Admin);
        var diff = new HoldingDiff(Guid.NewGuid(), Guid.NewGuid(), []);
        serviceMock.Setup(s => s.GetLatestDiffAsync()).ReturnsAsync(diff);

        var result = await controller.Index();

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<TableViewModel>();
        var expected = ColorService.GetAdminColors();
        model.ColorScheme.New.ShouldBe(expected.New);
    }
}
