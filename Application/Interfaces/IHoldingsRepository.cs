using Domain;

namespace Application.Interfaces;

public interface IHoldingsRepository
{
    Task SaveSnapshotAsync(HoldingSnapshot snapshot);
    Task<HoldingSnapshot?> GetLatestSnapshotAsync();

    Task SaveDiffAsync(HoldingDiff diff);
    Task<HoldingDiff?> GetLatestDiffAsync();

    // For export, I understand that the user will select a range of dates to export?
    Task<IReadOnlyList<HoldingDiff>> GetDiffsInRangeAsync(DateTime from, DateTime to);

    Task DeleteOldDataAsync(DateTime cutoff);
}
