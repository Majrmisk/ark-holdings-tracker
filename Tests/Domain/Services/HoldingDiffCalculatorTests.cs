using Domain;
using Domain.Services;
using Shouldly;

namespace Tests.Domain.Services;

public class HoldingDiffCalculatorTests
{
    private static readonly HoldingRecord TeslaRecord =
        new("Tesla Inc", "TSLA", "88160R101", 3_000_000, 750_000_000m, 10.50m);

    private static readonly HoldingRecord CoinbaseRecord =
        new("Coinbase Global Inc", "COIN", "19260Q107", 1_500_000, 300_000_000m, 4.20m);

    private static readonly HoldingRecord RokuRecord =
        new("Roku Inc", "ROKU", "77543R102", 500_000, 50_000_000m, 2.00m);

    [Fact]
    public void Compute_ReturnsNewEntry_WhenHoldingOnlyInCurrentSnapshot()
    {
        var previous = new HoldingSnapshot(DateTimeOffset.UtcNow, []);
        var current = new HoldingSnapshot(DateTimeOffset.UtcNow, [TeslaRecord]);

        var diff = HoldingDiffCalculator.Compute(previous, current);

        diff.Entries.Count.ShouldBe(1);
        var entry = diff.Entries[0];
        entry.Type.ShouldBe(DiffType.New);
        entry.Company.ShouldBe("Tesla Inc");
        entry.Ticker.ShouldBe("TSLA");
        entry.Cusip.ShouldBe("88160R101");
        entry.Shares.ShouldBe(3_000_000);
        entry.WeightPercent.ShouldBe(10.50m);
        entry.ChangePercent.ShouldBeNull();
    }

    [Fact]
    public void Compute_ReturnsRemovedEntry_WhenHoldingOnlyInPreviousSnapshot()
    {
        var previous = new HoldingSnapshot(DateTimeOffset.UtcNow, [TeslaRecord]);
        var current = new HoldingSnapshot(DateTimeOffset.UtcNow, []);

        var diff = HoldingDiffCalculator.Compute(previous, current);

        diff.Entries.Count.ShouldBe(1);
        var entry = diff.Entries[0];
        entry.Type.ShouldBe(DiffType.Removed);
        entry.Company.ShouldBe("Tesla Inc");
        entry.Shares.ShouldBe(0);
        entry.WeightPercent.ShouldBe(0);
        entry.ChangePercent.ShouldBe(-100m);
    }

    [Fact]
    public void Compute_ReturnsIncreasedEntry_WhenSharesGrow()
    {
        var previousRecord = new HoldingRecord("Tesla Inc", "TSLA", "88160R101", 1_000_000, 250_000_000m, 5.00m);
        var currentRecord = new HoldingRecord("Tesla Inc", "TSLA", "88160R101", 1_500_000, 375_000_000m, 7.50m);
        var previous = new HoldingSnapshot(DateTimeOffset.UtcNow, [previousRecord]);
        var current = new HoldingSnapshot(DateTimeOffset.UtcNow, [currentRecord]);

        var diff = HoldingDiffCalculator.Compute(previous, current);

        diff.Entries.Count.ShouldBe(1);
        var entry = diff.Entries[0];
        entry.Type.ShouldBe(DiffType.Increased);
        entry.ChangePercent.ShouldBe(50m);
    }

    [Fact]
    public void Compute_ReturnsReducedEntry_WhenSharesShrink()
    {
        var previousRecord = new HoldingRecord("Tesla Inc", "TSLA", "88160R101", 2_000_000, 500_000_000m, 10.00m);
        var currentRecord = new HoldingRecord("Tesla Inc", "TSLA", "88160R101", 1_000_000, 250_000_000m, 5.00m);
        var previous = new HoldingSnapshot(DateTimeOffset.UtcNow, [previousRecord]);
        var current = new HoldingSnapshot(DateTimeOffset.UtcNow, [currentRecord]);

        var diff = HoldingDiffCalculator.Compute(previous, current);

        diff.Entries.Count.ShouldBe(1);
        var entry = diff.Entries[0];
        entry.Type.ShouldBe(DiffType.Reduced);
        entry.ChangePercent.ShouldBe(-50m);
    }

    [Fact]
    public void Compute_ReturnsWeightShiftedEntry_WhenSharesSameButWeightDiffers()
    {
        var previousRecord = new HoldingRecord("Tesla Inc", "TSLA", "88160R101", 3_000_000, 750_000_000m, 10.50m);
        var currentRecord = new HoldingRecord("Tesla Inc", "TSLA", "88160R101", 3_000_000, 800_000_000m, 11.20m);
        var previous = new HoldingSnapshot(DateTimeOffset.UtcNow, [previousRecord]);
        var current = new HoldingSnapshot(DateTimeOffset.UtcNow, [currentRecord]);

        var diff = HoldingDiffCalculator.Compute(previous, current);

        diff.Entries.Count.ShouldBe(1);
        var entry = diff.Entries[0];
        entry.Type.ShouldBe(DiffType.WeightShifted);
        entry.ChangePercent.ShouldBe(0m);
    }

    [Fact]
    public void Compute_ReturnsUnchangedEntry_WhenSharesAndWeightIdentical()
    {
        var previous = new HoldingSnapshot(DateTimeOffset.UtcNow, [TeslaRecord]);
        var current = new HoldingSnapshot(DateTimeOffset.UtcNow, [TeslaRecord]);

        var diff = HoldingDiffCalculator.Compute(previous, current);

        diff.Entries.Count.ShouldBe(1);
        var entry = diff.Entries[0];
        entry.Type.ShouldBe(DiffType.Unchanged);
        entry.ChangePercent.ShouldBe(0m);
    }

    [Fact]
    public void Compute_ReturnsEmptyEntries_WhenBothSnapshotsEmpty()
    {
        var previous = new HoldingSnapshot(DateTimeOffset.UtcNow, []);
        var current = new HoldingSnapshot(DateTimeOffset.UtcNow, []);

        var diff = HoldingDiffCalculator.Compute(previous, current);

        diff.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Compute_HandlesMixedDiffTypes_InSingleCall()
    {
        var prevTesla = new HoldingRecord("Tesla Inc", "TSLA", "88160R101", 1_000_000, 250_000_000m, 5.00m);
        var previous = new HoldingSnapshot(DateTimeOffset.UtcNow, [prevTesla, CoinbaseRecord]);

        var currTesla = new HoldingRecord("Tesla Inc", "TSLA", "88160R101", 1_500_000, 375_000_000m, 7.50m);
        var current = new HoldingSnapshot(DateTimeOffset.UtcNow, [currTesla, RokuRecord]);

        var diff = HoldingDiffCalculator.Compute(previous, current);

        diff.Entries.Count.ShouldBe(3);
        diff.Entries.ShouldContain(e => e.Cusip == "88160R101" && e.Type == DiffType.Increased);
        diff.Entries.ShouldContain(e => e.Cusip == "19260Q107" && e.Type == DiffType.Removed);
        diff.Entries.ShouldContain(e => e.Cusip == "77543R102" && e.Type == DiffType.New);
    }

    [Fact]
    public void Compute_SetsCorrectSnapshotIds_OnReturnedDiff()
    {
        var previous = new HoldingSnapshot(DateTimeOffset.UtcNow, [TeslaRecord]);
        var current = new HoldingSnapshot(DateTimeOffset.UtcNow, [TeslaRecord]);

        var diff = HoldingDiffCalculator.Compute(previous, current);

        diff.PreviousSnapshotId.ShouldBe(previous.Id);
        diff.CurrentSnapshotId.ShouldBe(current.Id);
    }

    [Fact]
    public void Compute_CalculatesChangePercent_WhenSharesDouble()
    {
        var previousRecord = new HoldingRecord("Tesla Inc", "TSLA", "88160R101", 1_000, 250_000m, 5.00m);
        var currentRecord = new HoldingRecord("Tesla Inc", "TSLA", "88160R101", 2_000, 500_000m, 10.00m);
        var previous = new HoldingSnapshot(DateTimeOffset.UtcNow, [previousRecord]);
        var current = new HoldingSnapshot(DateTimeOffset.UtcNow, [currentRecord]);

        var diff = HoldingDiffCalculator.Compute(previous, current);

        diff.Entries[0].ChangePercent.ShouldBe(100m);
    }

    [Fact]
    public void Compute_CalculatesChangePercent_WhenSharesHalved()
    {
        var previousRecord = new HoldingRecord("Tesla Inc", "TSLA", "88160R101", 2_000, 500_000m, 10.00m);
        var currentRecord = new HoldingRecord("Tesla Inc", "TSLA", "88160R101", 1_000, 250_000m, 5.00m);
        var previous = new HoldingSnapshot(DateTimeOffset.UtcNow, [previousRecord]);
        var current = new HoldingSnapshot(DateTimeOffset.UtcNow, [currentRecord]);

        var diff = HoldingDiffCalculator.Compute(previous, current);

        diff.Entries[0].ChangePercent.ShouldBe(-50m);
    }

    [Fact]
    public void Compute_UsesCurrentCompanyAndTicker_WhenHoldingExistsInBoth()
    {
        var previousRecord = new HoldingRecord("Old Name", "OLD", "88160R101", 1_000_000, 250_000_000m, 5.00m);
        var currentRecord = new HoldingRecord("New Name", "NEW", "88160R101", 1_000_000, 250_000_000m, 5.00m);
        var previous = new HoldingSnapshot(DateTimeOffset.UtcNow, [previousRecord]);
        var current = new HoldingSnapshot(DateTimeOffset.UtcNow, [currentRecord]);

        var diff = HoldingDiffCalculator.Compute(previous, current);

        diff.Entries[0].Company.ShouldBe("New Name");
        diff.Entries[0].Ticker.ShouldBe("NEW");
    }

    [Fact]
    public void Compute_ReturnsAllNewEntries_WhenPreviousSnapshotIsEmpty()
    {
        var previous = new HoldingSnapshot(DateTimeOffset.UtcNow, []);
        var current = new HoldingSnapshot(DateTimeOffset.UtcNow, [TeslaRecord, CoinbaseRecord, RokuRecord]);

        var diff = HoldingDiffCalculator.Compute(previous, current);

        diff.Entries.Count.ShouldBe(3);
        diff.Entries.ShouldAllBe(e => e.Type == DiffType.New);
    }

    [Fact]
    public void Compute_ReturnsAllRemovedEntries_WhenCurrentSnapshotIsEmpty()
    {
        var previous = new HoldingSnapshot(DateTimeOffset.UtcNow, [TeslaRecord, CoinbaseRecord, RokuRecord]);
        var current = new HoldingSnapshot(DateTimeOffset.UtcNow, []);

        var diff = HoldingDiffCalculator.Compute(previous, current);

        diff.Entries.Count.ShouldBe(3);
        diff.Entries.ShouldAllBe(e => e.Type == DiffType.Removed);
    }
}
