using Domain;
using Infrastructure.Repositories;
using Marten;
using Shouldly;
using Tests.TestHelpers;

namespace Tests.Infrastructure.Repositories;

public class HoldingsRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public HoldingsRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<IDocumentSession> CreateCleanSessionAsync()
    {
        using var store = _fixture.CreateDocumentStore();
        await using var cleanSession = store.LightweightSession();
        cleanSession.DeleteWhere<HoldingSnapshot>(_ => true);
        cleanSession.DeleteWhere<HoldingDiff>(_ => true);
        await cleanSession.SaveChangesAsync();

        return store.LightweightSession();
    }

    [Fact]
    public async Task SaveSnapshotAsync_PersistsSnapshot_WhenValid()
    {
        using var store = _fixture.CreateDocumentStore();
        await using var session = store.LightweightSession();
        session.DeleteWhere<HoldingSnapshot>(_ => true);
        await session.SaveChangesAsync();

        var repo = new HoldingsRepository(session);
        var record = new HoldingRecord("Tesla Inc", "TSLA", "88160R101", 3_000_000, 750_000_000m, 10.50m);
        var snapshot = new HoldingSnapshot(DateTimeOffset.UtcNow, [record]);

        await repo.SaveSnapshotAsync(snapshot);

        await using var querySession = store.LightweightSession();
        var retrieved = await querySession.Query<HoldingSnapshot>()
            .Where(s => s.Id == snapshot.Id)
            .FirstOrDefaultAsync();
        retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe(snapshot.Id);
        retrieved.Holdings.Count.ShouldBe(1);
        retrieved.Holdings[0].Company.ShouldBe("Tesla Inc");
    }

    [Fact]
    public async Task GetLatestSnapshotAsync_ReturnsNull_WhenNoSnapshots()
    {
        using var store = _fixture.CreateDocumentStore();
        await using var session = store.LightweightSession();
        session.DeleteWhere<HoldingSnapshot>(_ => true);
        await session.SaveChangesAsync();

        var repo = new HoldingsRepository(session);

        var result = await repo.GetLatestSnapshotAsync();

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetLatestSnapshotAsync_ReturnsMostRecent_WhenMultipleExist()
    {
        using var store = _fixture.CreateDocumentStore();
        await using var session = store.LightweightSession();
        session.DeleteWhere<HoldingSnapshot>(_ => true);
        await session.SaveChangesAsync();

        var repo = new HoldingsRepository(session);
        var oldest = new HoldingSnapshot(DateTimeOffset.UtcNow.AddHours(-3), []);
        var middle = new HoldingSnapshot(DateTimeOffset.UtcNow.AddHours(-2), []);
        var newest = new HoldingSnapshot(DateTimeOffset.UtcNow.AddHours(-1), []);

        session.Store(oldest);
        session.Store(middle);
        session.Store(newest);
        await session.SaveChangesAsync();

        var result = await repo.GetLatestSnapshotAsync();

        result.ShouldNotBeNull();
        result.Id.ShouldBe(newest.Id);
    }

    [Fact]
    public async Task SaveDiffAsync_PersistsDiff_WhenValid()
    {
        using var store = _fixture.CreateDocumentStore();
        await using var session = store.LightweightSession();
        session.DeleteWhere<HoldingDiff>(_ => true);
        await session.SaveChangesAsync();

        var repo = new HoldingsRepository(session);
        var prevId = Guid.NewGuid();
        var currId = Guid.NewGuid();
        var entry = new DiffEntry("Tesla Inc", "TSLA", "88160R101", 3_000_000, 10.50m, DiffType.New, null);
        var diff = new HoldingDiff(prevId, currId, [entry]);

        await repo.SaveDiffAsync(diff);

        await using var querySession = store.LightweightSession();
        var retrieved = await querySession.Query<HoldingDiff>()
            .Where(d => d.Id == diff.Id)
            .FirstOrDefaultAsync();
        retrieved.ShouldNotBeNull();
        retrieved.PreviousSnapshotId.ShouldBe(prevId);
        retrieved.CurrentSnapshotId.ShouldBe(currId);
        retrieved.Entries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetLatestDiffAsync_ReturnsNull_WhenNoDiffs()
    {
        using var store = _fixture.CreateDocumentStore();
        await using var session = store.LightweightSession();
        session.DeleteWhere<HoldingDiff>(_ => true);
        await session.SaveChangesAsync();

        var repo = new HoldingsRepository(session);

        var result = await repo.GetLatestDiffAsync();

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetLatestDiffAsync_ReturnsMostRecent_WhenMultipleExist()
    {
        using var store = _fixture.CreateDocumentStore();
        await using var session = store.LightweightSession();
        session.DeleteWhere<HoldingDiff>(_ => true);
        await session.SaveChangesAsync();

        var repo = new HoldingsRepository(session);
        var oldest = new HoldingDiff(Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-3), Guid.NewGuid(), Guid.NewGuid(), []);
        var newest = new HoldingDiff(Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-1), Guid.NewGuid(), Guid.NewGuid(), []);

        session.Store(oldest);
        session.Store(newest);
        await session.SaveChangesAsync();

        var result = await repo.GetLatestDiffAsync();

        result.ShouldNotBeNull();
        result.Id.ShouldBe(newest.Id);
    }

    [Fact]
    public async Task GetDiffsInRangeAsync_ReturnsOnlyDiffsWithinRange()
    {
        using var store = _fixture.CreateDocumentStore();
        await using var session = store.LightweightSession();
        session.DeleteWhere<HoldingDiff>(_ => true);
        await session.SaveChangesAsync();

        var repo = new HoldingsRepository(session);
        var before = new HoldingDiff(Guid.NewGuid(), new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), Guid.NewGuid(), Guid.NewGuid(), []);
        var within = new HoldingDiff(Guid.NewGuid(), new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero), Guid.NewGuid(), Guid.NewGuid(), []);
        var after = new HoldingDiff(Guid.NewGuid(), new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero), Guid.NewGuid(), Guid.NewGuid(), []);

        session.Store(before);
        session.Store(within);
        session.Store(after);
        await session.SaveChangesAsync();

        var from = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        var result = await repo.GetDiffsInRangeAsync(from, to);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(within.Id);
    }

    [Fact]
    public async Task GetDiffsInRangeAsync_ReturnsEmptyList_WhenNoDiffsInRange()
    {
        using var store = _fixture.CreateDocumentStore();
        await using var session = store.LightweightSession();
        session.DeleteWhere<HoldingDiff>(_ => true);
        await session.SaveChangesAsync();

        var repo = new HoldingsRepository(session);
        var diff = new HoldingDiff(Guid.NewGuid(), new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), Guid.NewGuid(), Guid.NewGuid(), []);
        session.Store(diff);
        await session.SaveChangesAsync();

        var from = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        var result = await repo.GetDiffsInRangeAsync(from, to);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDiffsInRangeAsync_ReturnsDiffsOrderedByComputedAt()
    {
        using var store = _fixture.CreateDocumentStore();
        await using var session = store.LightweightSession();
        session.DeleteWhere<HoldingDiff>(_ => true);
        await session.SaveChangesAsync();

        var repo = new HoldingsRepository(session);
        var later = new HoldingDiff(Guid.NewGuid(), new DateTimeOffset(2025, 6, 20, 0, 0, 0, TimeSpan.Zero), Guid.NewGuid(), Guid.NewGuid(), []);
        var earlier = new HoldingDiff(Guid.NewGuid(), new DateTimeOffset(2025, 6, 10, 0, 0, 0, TimeSpan.Zero), Guid.NewGuid(), Guid.NewGuid(), []);

        session.Store(later);
        session.Store(earlier);
        await session.SaveChangesAsync();

        var from = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        var result = await repo.GetDiffsInRangeAsync(from, to);

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(earlier.Id);
        result[1].Id.ShouldBe(later.Id);
    }

    [Fact]
    public async Task DeleteOldDataAsync_RemovesOnlyDataBeforeCutoff()
    {
        using var store = _fixture.CreateDocumentStore();
        await using var session = store.LightweightSession();
        session.DeleteWhere<HoldingSnapshot>(_ => true);
        session.DeleteWhere<HoldingDiff>(_ => true);
        await session.SaveChangesAsync();

        var repo = new HoldingsRepository(session);
        var cutoff = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        var oldSnapshot = new HoldingSnapshot(new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero), []);
        var newSnapshot = new HoldingSnapshot(new DateTimeOffset(2025, 6, 20, 0, 0, 0, TimeSpan.Zero), []);
        var oldDiff = new HoldingDiff(Guid.NewGuid(), new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero), Guid.NewGuid(), Guid.NewGuid(), []);
        var newDiff = new HoldingDiff(Guid.NewGuid(), new DateTimeOffset(2025, 6, 20, 0, 0, 0, TimeSpan.Zero), Guid.NewGuid(), Guid.NewGuid(), []);

        session.Store(oldSnapshot);
        session.Store(newSnapshot);
        session.Store(oldDiff);
        session.Store(newDiff);
        await session.SaveChangesAsync();

        await repo.DeleteOldDataAsync(cutoff);

        await using var querySession = store.LightweightSession();
        var snapshots = await querySession.Query<HoldingSnapshot>().ToListAsync();
        var diffs = await querySession.Query<HoldingDiff>().ToListAsync();

        snapshots.Count.ShouldBe(1);
        snapshots[0].Id.ShouldBe(newSnapshot.Id);
        diffs.Count.ShouldBe(1);
        diffs[0].Id.ShouldBe(newDiff.Id);
    }
}
