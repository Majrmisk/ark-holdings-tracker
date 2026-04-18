using Application.Interfaces;
using Domain;
using Marten;

namespace Infrastructure.Repositories;

public class HoldingsRepository(IDocumentSession session) : IHoldingsRepository
{
    public async Task SaveSnapshotAsync(HoldingSnapshot snapshot)
    {
        session.Store(snapshot);
        await session.SaveChangesAsync();
    }

    public async Task<HoldingSnapshot?> GetLatestSnapshotAsync()
    {
        return await session.Query<HoldingSnapshot>()
            .OrderByDescending(s => s.FetchedAt)
            .FirstOrDefaultAsync();
    }

    public async Task SaveDiffAsync(HoldingDiff diff)
    {
        session.Store(diff);
        await session.SaveChangesAsync();
    }

    public async Task<HoldingDiff?> GetLatestDiffAsync()
    {
        return await session.Query<HoldingDiff>()
            .OrderByDescending(d => d.ComputedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<HoldingDiff>> GetDiffsInRangeAsync(DateTime from, DateTime to)
    {
        return await session.Query<HoldingDiff>()
            .Where(d => d.ComputedAt >= from && d.ComputedAt <= to)
            .OrderBy(d => d.ComputedAt)
            .ToListAsync();
    }

    public async Task DeleteOldDataAsync(DateTime cutoff)
    {
        session.DeleteWhere<HoldingDiff>(d => d.ComputedAt < cutoff);
        await session.SaveChangesAsync();

        session.DeleteWhere<HoldingSnapshot>(s => s.FetchedAt < cutoff);
        await session.SaveChangesAsync();
    }
}
