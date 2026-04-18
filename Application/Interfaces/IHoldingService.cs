using Domain;

namespace Application.Interfaces
{
    public interface IHoldingService
    {
        Task FetchLatest();
        Task<HoldingSnapshot?> GetLatestSnapshotAsync();
        Task<HoldingDiff?> GetLatestDiffAsync();
        Task<IReadOnlyList<HoldingDiff>> GetDiffsInRangeAsync(DateTime from, DateTime to);
        Task DeleteOldDataAsync();
    }
}
