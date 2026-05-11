using Presentation.Controllers;
using Presentation.Models;
using Application.Interfaces;
using Application.Services;
using Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;
using System.Security.Claims;

namespace Tests.Presentation.Controllers;

public class TableControllerTests
{
    private static (TableController Controller, Mock<IAuthService> AuthServiceMock,
        Mock<IHoldingService> HoldingServiceMock) CreateController(
        UserRole callerRole = UserRole.User)
    {
        var authServiceMock = new Mock<IAuthService>();
        var holdingServiceMock = new Mock<IHoldingService>();

        authServiceMock.Setup(a => a.GetCurrentUserRoleAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(callerRole);

        var controller = new TableController(authServiceMock.Object, holdingServiceMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "caller-1")], "TestAuth"))
            }
        };
        return (controller, authServiceMock, holdingServiceMock);
    }

    [Fact]
    public async Task Index_ReturnsViewWithEmptyTableViewModel_WhenDiffIsNull()
    {
        var (controller, _, holdingServiceMock) = CreateController();
        holdingServiceMock.Setup(s => s.GetLatestDiffAsync()).ReturnsAsync((HoldingDiff?)null);

        var result = await controller.Index();

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<TableViewModel>();
        model.Diff.ShouldBeNull();
    }

    [Fact]
    public async Task Index_ReturnsViewWithDiff_WhenDiffExists()
    {
        var (controller, _, holdingServiceMock) = CreateController();
        var diff = new HoldingDiff(Guid.NewGuid(), Guid.NewGuid(), []);
        holdingServiceMock.Setup(s => s.GetLatestDiffAsync()).ReturnsAsync(diff);

        var result = await controller.Index();

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<TableViewModel>();
        model.Diff.ShouldBe(diff);
        model.ColorsEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task Index_SetsCanToggleColorsTrue_WhenRoleIsPowerUser()
    {
        var (controller, _, holdingServiceMock) = CreateController(UserRole.PowerUser);
        var diff = new HoldingDiff(Guid.NewGuid(), Guid.NewGuid(), []);
        holdingServiceMock.Setup(s => s.GetLatestDiffAsync()).ReturnsAsync(diff);

        var result = await controller.Index();

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<TableViewModel>();
        model.CanToggleColors.ShouldBeTrue();
    }

    [Fact]
    public async Task Index_SetsCanToggleColorsFalse_WhenRoleIsNotPowerUser()
    {
        var (controller, _, holdingServiceMock) = CreateController(UserRole.Admin);
        var diff = new HoldingDiff(Guid.NewGuid(), Guid.NewGuid(), []);
        holdingServiceMock.Setup(s => s.GetLatestDiffAsync()).ReturnsAsync(diff);

        var result = await controller.Index();

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<TableViewModel>();
        model.CanToggleColors.ShouldBeFalse();
    }

    [Fact]
    public async Task Index_UsesColorSchemeByRole_WhenColorsEnabled()
    {
        var (controller, _, holdingServiceMock) = CreateController(UserRole.Admin);
        var diff = new HoldingDiff(Guid.NewGuid(), Guid.NewGuid(), []);
        holdingServiceMock.Setup(s => s.GetLatestDiffAsync()).ReturnsAsync(diff);

        var result = await controller.Index(colors: true);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<TableViewModel>();
        var expected = ColorService.GetColorSchemeByRole(UserRole.Admin);
        model.ColorScheme.New.ShouldBe(expected.New);
        model.ColorScheme.Increased.ShouldBe(expected.Increased);
        model.ColorScheme.Reduced.ShouldBe(expected.Reduced);
        model.ColorScheme.Removed.ShouldBe(expected.Removed);
        model.ColorScheme.Unchanged.ShouldBe(expected.Unchanged);
        model.ColorScheme.WeightShifted.ShouldBe(expected.WeightShifted);
    }

    [Fact]
    public async Task Index_UsesDefaultColorscheme_WhenColorsDisabled()
    {
        var (controller, _, holdingServiceMock) = CreateController(UserRole.Admin);
        var diff = new HoldingDiff(Guid.NewGuid(), Guid.NewGuid(), []);
        holdingServiceMock.Setup(s => s.GetLatestDiffAsync()).ReturnsAsync(diff);

        var result = await controller.Index(colors: false);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<TableViewModel>();
        var defaultScheme = new Colorscheme();
        model.ColorScheme.New.ShouldBe(defaultScheme.New);
        model.ColorScheme.Increased.ShouldBe(defaultScheme.Increased);
        model.ColorsEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task Index_DoesNotCallAuthService_WhenDiffIsNull()
    {
        var (controller, authServiceMock, holdingServiceMock) = CreateController();
        holdingServiceMock.Setup(s => s.GetLatestDiffAsync()).ReturnsAsync((HoldingDiff?)null);

        await controller.Index();

        authServiceMock.Verify(
            a => a.GetCurrentUserRoleAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }
}
