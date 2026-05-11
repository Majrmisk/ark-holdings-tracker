using Presentation.Controllers;
using Presentation.Models;
using Application.Interfaces;
using Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;
using Tests.TestHelpers;

namespace Tests.Presentation.Controllers;

public class HomeControllerTests
{
    private static (HomeController Controller, Mock<IHoldingService> ServiceMock,
        SpyLogger<HomeController> Logger) CreateController()
    {
        var serviceMock = new Mock<IHoldingService>();
        var logger = new SpyLogger<HomeController>();
        var controller = new HomeController(logger, serviceMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return (controller, serviceMock, logger);
    }

    [Fact]
    public async Task Index_ReturnsView_WhenSnapshotAndDiffExist()
    {
        var (controller, serviceMock, _) = CreateController();
        var snapshot = new HoldingSnapshot(DateTimeOffset.UtcNow, []);
        var diff = new HoldingDiff(Guid.NewGuid(), Guid.NewGuid(), []);
        serviceMock.Setup(s => s.GetLatestSnapshotAsync()).ReturnsAsync(snapshot);
        serviceMock.Setup(s => s.GetLatestDiffAsync()).ReturnsAsync(diff);

        var result = await controller.Index();

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task Index_ReturnsView_WhenSnapshotIsNull()
    {
        var (controller, serviceMock, logger) = CreateController();
        serviceMock.Setup(s => s.GetLatestSnapshotAsync()).ReturnsAsync((HoldingSnapshot?)null);
        serviceMock.Setup(s => s.GetLatestDiffAsync()).ReturnsAsync((HoldingDiff?)null);

        var result = await controller.Index();

        result.ShouldBeOfType<ViewResult>();
        logger.Entries.ShouldContain(e => e.Message.Contains("None latest snapshot"));
    }

    [Fact]
    public async Task Index_ReturnsView_WhenDiffIsNull()
    {
        var (controller, serviceMock, logger) = CreateController();
        var snapshot = new HoldingSnapshot(DateTimeOffset.UtcNow, []);
        serviceMock.Setup(s => s.GetLatestSnapshotAsync()).ReturnsAsync(snapshot);
        serviceMock.Setup(s => s.GetLatestDiffAsync()).ReturnsAsync((HoldingDiff?)null);

        var result = await controller.Index();

        result.ShouldBeOfType<ViewResult>();
        logger.Entries.ShouldContain(e => e.Message.Contains("No latest diff"));
    }

    [Fact]
    public void Privacy_ReturnsView()
    {
        var (controller, _, _) = CreateController();

        var result = controller.Privacy();

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public void Error_SetsStatusCode_WhenProvided()
    {
        var (controller, _, _) = CreateController();

        var result = controller.Error(404);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<ErrorViewModel>();
        model.RequestId.ShouldNotBeNull();
        controller.HttpContext.Response.StatusCode.ShouldBe(404);
    }
}
