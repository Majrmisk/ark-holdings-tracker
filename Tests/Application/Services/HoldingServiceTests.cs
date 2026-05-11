using Application.Interfaces;
using Application.Services;
using Application.Settings;
using Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Tests.TestHelpers;

namespace Tests.Application.Services;

public class HoldingServiceTests
{
    private static (HoldingService Service, Mock<IHoldingsRepository> RepoMock,
        Mock<IHoldingClient> ClientMock, SpyLogger<HoldingService> Logger) CreateService(
        int deleteAfterDays = 14)
    {
        var repoMock = new Mock<IHoldingsRepository>();
        var clientMock = new Mock<IHoldingClient>();
        var logger = new SpyLogger<HoldingService>();
        var options = Options.Create(new DataRetentionSettings { DeleteAfterDays = deleteAfterDays });
        var service = new HoldingService(repoMock.Object, clientMock.Object, options, logger);
        return (service, repoMock, clientMock, logger);
    }

    [Fact]
    public async Task FetchLatest_SavesSnapshotAndSelfDiff_WhenNoPreviousSnapshotExists()
    {
        var (service, repoMock, clientMock, _) = CreateService();
        var snapshot = new HoldingSnapshot(DateTimeOffset.UtcNow, []);
        clientMock.Setup(c => c.Fetch()).ReturnsAsync(snapshot);
        repoMock.Setup(r => r.GetLatestSnapshotAsync()).ReturnsAsync((HoldingSnapshot?)null);

        await service.FetchLatest();

        repoMock.Verify(r => r.SaveSnapshotAsync(snapshot), Times.Once);
        repoMock.Verify(r => r.SaveDiffAsync(It.Is<HoldingDiff>(d =>
            d.PreviousSnapshotId == snapshot.Id &&
            d.CurrentSnapshotId == snapshot.Id)), Times.Once);
    }

    [Fact]
    public async Task FetchLatest_SavesSnapshotAndDiff_WhenPreviousSnapshotDiffers()
    {
        var (service, repoMock, clientMock, _) = CreateService();

        var previousSnapshot = new HoldingSnapshot(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(-1),
            new[]
            {
                new HoldingRecord("A", "A", "CUSIP1", 10, 100, 100)
            });

        var newSnapshot = new HoldingSnapshot(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            new[]
            {
                new HoldingRecord("A", "A", "CUSIP1", 20, 200, 100)
            });

        clientMock.Setup(c => c.Fetch()).ReturnsAsync(newSnapshot);
        repoMock.Setup(r => r.GetLatestSnapshotAsync()).ReturnsAsync(previousSnapshot);

        await service.FetchLatest();

        repoMock.Verify(r => r.SaveSnapshotAsync(newSnapshot), Times.Once);
        repoMock.Verify(r => r.SaveDiffAsync(It.IsAny<HoldingDiff>()), Times.Once);
    }

    [Fact]
    public async Task FetchLatest_DoesNotPersist_WhenSnapshotUnchanged()
    {
        var (service, repoMock, clientMock, _) = CreateService();

        var snapshot = new HoldingSnapshot(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            new[]
            {
                new HoldingRecord("A", "A", "CUSIP1", 10, 100, 100)
            });

        clientMock.Setup(c => c.Fetch()).ReturnsAsync(snapshot);
        repoMock.Setup(r => r.GetLatestSnapshotAsync()).ReturnsAsync(snapshot);

        await service.FetchLatest();

        repoMock.Verify(r => r.SaveSnapshotAsync(It.IsAny<HoldingSnapshot>()), Times.Never);
        repoMock.Verify(r => r.SaveDiffAsync(It.IsAny<HoldingDiff>()), Times.Never);
    }

    [Fact]
    public async Task FetchLatest_LogsWarning_WhenFetchReturnsNull()
    {
        var (service, _, clientMock, logger) = CreateService();
        clientMock.Setup(c => c.Fetch()).ReturnsAsync((HoldingSnapshot?)null);

        await service.FetchLatest();

        logger.HasWarningOrAbove.ShouldBeTrue();
        logger.Entries.ShouldContain(e =>
            e.Level == LogLevel.Warning &&
            e.Message.Contains("Holding fetch returned no snapshot. Data was not updated."));
    }

    [Fact]
    public async Task FetchLatest_DoesNotSaveAnything_WhenFetchReturnsNull()
    {
        var (service, repoMock, clientMock, _) = CreateService();
        clientMock.Setup(c => c.Fetch()).ReturnsAsync((HoldingSnapshot?)null);

        await service.FetchLatest();

        repoMock.Verify(r => r.SaveSnapshotAsync(It.IsAny<HoldingSnapshot>()), Times.Never);
        repoMock.Verify(r => r.SaveDiffAsync(It.IsAny<HoldingDiff>()), Times.Never);
    }

    [Fact]
    public async Task FetchLatest_PassesFetchedSnapshotToSave()
    {
        var (service, repoMock, clientMock, _) = CreateService();
        var snapshot = new HoldingSnapshot(DateTimeOffset.UtcNow, []);
        clientMock.Setup(c => c.Fetch()).ReturnsAsync(snapshot);
        repoMock.Setup(r => r.GetLatestSnapshotAsync()).ReturnsAsync((HoldingSnapshot?)null);

        await service.FetchLatest();

        repoMock.Verify(r => r.SaveSnapshotAsync(snapshot), Times.Once);
    }

    [Fact]
    public async Task FetchLatest_ComputesDiffWithCorrectSnapshotIds_WhenPreviousExists()
    {
        var (service, repoMock, clientMock, _) = CreateService();

        var previousSnapshot = new HoldingSnapshot(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(-1),
            new[]
            {
                new HoldingRecord("A", "A", "CUSIP1", 10, 100, 100)
            });

        var newSnapshot = new HoldingSnapshot(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            new[]
            {
                new HoldingRecord("A", "A", "CUSIP1", 20, 200, 100)
            });

        clientMock.Setup(c => c.Fetch()).ReturnsAsync(newSnapshot);
        repoMock.Setup(r => r.GetLatestSnapshotAsync()).ReturnsAsync(previousSnapshot);

        await service.FetchLatest();

        repoMock.Verify(r => r.SaveDiffAsync(It.Is<HoldingDiff>(d =>
            d.PreviousSnapshotId == previousSnapshot.Id &&
            d.CurrentSnapshotId == newSnapshot.Id)), Times.Once);
    }

    [Fact]
    public async Task GetLatestSnapshotAsync_DelegatesToRepository()
    {
        var (service, repoMock, _, _) = CreateService();
        var expected = new HoldingSnapshot(DateTimeOffset.UtcNow, []);
        repoMock.Setup(r => r.GetLatestSnapshotAsync()).ReturnsAsync(expected);

        var result = await service.GetLatestSnapshotAsync();

        result.ShouldBe(expected);
        repoMock.Verify(r => r.GetLatestSnapshotAsync(), Times.Once);
    }

    [Fact]
    public async Task GetLatestSnapshotAsync_ReturnsNull_WhenRepositoryReturnsNull()
    {
        var (service, repoMock, _, _) = CreateService();
        repoMock.Setup(r => r.GetLatestSnapshotAsync()).ReturnsAsync((HoldingSnapshot?)null);

        var result = await service.GetLatestSnapshotAsync();

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetLatestDiffAsync_DelegatesToRepository()
    {
        var (service, repoMock, _, _) = CreateService();
        var expected = new HoldingDiff(Guid.NewGuid(), Guid.NewGuid(), []);
        repoMock.Setup(r => r.GetLatestDiffAsync()).ReturnsAsync(expected);

        var result = await service.GetLatestDiffAsync();

        result.ShouldBe(expected);
        repoMock.Verify(r => r.GetLatestDiffAsync(), Times.Once);
    }

    [Fact]
    public async Task GetLatestDiffAsync_ReturnsNull_WhenRepositoryReturnsNull()
    {
        var (service, repoMock, _, _) = CreateService();
        repoMock.Setup(r => r.GetLatestDiffAsync()).ReturnsAsync((HoldingDiff?)null);

        var result = await service.GetLatestDiffAsync();

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetDiffsInRangeAsync_DelegatesToRepository_WithCorrectParameters()
    {
        var (service, repoMock, _, _) = CreateService();
        var from = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var expected = new List<HoldingDiff> { new(Guid.NewGuid(), Guid.NewGuid(), []) };
        repoMock.Setup(r => r.GetDiffsInRangeAsync(from, to)).ReturnsAsync(expected);

        var result = await service.GetDiffsInRangeAsync(from, to);

        result.ShouldBe(expected);
        repoMock.Verify(r => r.GetDiffsInRangeAsync(from, to), Times.Once);
    }

    [Fact]
    public async Task DeleteOldDataAsync_CallsRepositoryWithCorrectCutoff()
    {
        var (service, repoMock, _, _) = CreateService(deleteAfterDays: 14);
        var before = DateTime.UtcNow.AddDays(-14);

        await service.DeleteOldDataAsync();

        repoMock.Verify(r => r.DeleteOldDataAsync(It.Is<DateTime>(d =>
            d >= before.AddSeconds(-5) && d <= DateTime.UtcNow.AddDays(-14).AddSeconds(5))), Times.Once);
    }

    [Fact]
    public async Task DeleteOldDataAsync_UsesConfiguredRetentionDays()
    {
        var (service, repoMock, _, _) = CreateService(deleteAfterDays: 7);
        var before = DateTime.UtcNow.AddDays(-7);

        await service.DeleteOldDataAsync();

        repoMock.Verify(r => r.DeleteOldDataAsync(It.Is<DateTime>(d =>
            d >= before.AddSeconds(-5) && d <= DateTime.UtcNow.AddDays(-7).AddSeconds(5))), Times.Once);
    }
}
