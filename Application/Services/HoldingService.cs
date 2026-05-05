using Application.Interfaces;
using Application.Settings;
using Domain;
using Microsoft.Extensions.Options;

namespace Application.Services
{
    public class HoldingService : IHoldingService
    {
        private readonly IHoldingsRepository _holdingsRepository;
        private readonly IHoldingClient _holdingClient;
        private readonly int _deleteAfterDays;

        public HoldingService(IHoldingsRepository holdingsRepository, IHoldingClient holdingClient, IOptions<DataRetentionSettings> options)
        {
            _holdingsRepository = holdingsRepository;
            _holdingClient = holdingClient;
            _deleteAfterDays = options.Value.DeleteAfterDays;
        }

        public async Task<HoldingSnapshot?> GetLatestSnapshotAsync()
        {
            return await _holdingsRepository.GetLatestSnapshotAsync();
        }

        public async Task<HoldingDiff?> GetLatestDiffAsync()
        {
            return await _holdingsRepository.GetLatestDiffAsync();
        }

        public async Task<IReadOnlyList<HoldingDiff>> GetDiffsInRangeAsync(DateTime from, DateTime to)
        {
            return await _holdingsRepository.GetDiffsInRangeAsync(from, to);
        }

        public async Task DeleteOldDataAsync()
        {
            var cutoff = DateTime.UtcNow.AddDays(-_deleteAfterDays);
            await _holdingsRepository.DeleteOldDataAsync(cutoff);
        }

        public async Task FetchLatest()
        {
            var snapshot = await _holdingClient.Fetch();
            if (snapshot == null)
                return;

            var latest = await _holdingsRepository.GetLatestSnapshotAsync();
            if (latest != null)
            {
                var diff = ComputeDiff(latest, snapshot);
                await _holdingsRepository.SaveDiffAsync(diff);
            }

            await _holdingsRepository.SaveSnapshotAsync(snapshot);
        }

        private static HoldingDiff ComputeDiff(HoldingSnapshot previous, HoldingSnapshot current)
        {
            var entries = new List<DiffEntry>();

            var previousMap = previous.Holdings.ToDictionary(h => h.Cusip);
            var currentMap = current.Holdings.ToDictionary(h => h.Cusip);

            foreach (var (cusip, prev) in previousMap)
            {
                if (currentMap.TryGetValue(cusip, out var curr))
                {
                    if (curr.Shares == prev.Shares)
                    {
                        entries.Add(new DiffEntry
                        {
                            Company = curr.Company,
                            Ticker = curr.Ticker,
                            Cusip = cusip,
                            Shares = curr.Shares,
                            WeightPercent = curr.WeightPercent,
                            Type = curr.WeightPercent != prev.WeightPercent ? DiffType.WeightShifted : DiffType.Unchanged,
                            ChangePercent = 0m
                        });
                        continue;
                    }

                    var changePercent = (decimal)(curr.Shares - prev.Shares) / prev.Shares * 100;

                    entries.Add(new DiffEntry
                    {
                        Company = curr.Company,
                        Ticker = curr.Ticker,
                        Cusip = cusip,
                        Shares = curr.Shares,
                        WeightPercent = curr.WeightPercent,
                        Type = curr.Shares > prev.Shares ? DiffType.Increased : DiffType.Reduced,
                        ChangePercent = changePercent
                    });
                }
                else
                {
                    entries.Add(new DiffEntry
                    {
                        Company = prev.Company,
                        Ticker = prev.Ticker,
                        Cusip = cusip,
                        Shares = 0,
                        WeightPercent = 0,
                        Type = DiffType.Removed,
                        ChangePercent = -100m
                    });
                }
            }

            foreach (var (cusip, curr) in currentMap.Where(c => !previousMap.ContainsKey(c.Key)))
            {
                entries.Add(new DiffEntry
                {
                    Company = curr.Company,
                    Ticker = curr.Ticker,
                    Cusip = cusip,
                    Shares = curr.Shares,
                    WeightPercent = curr.WeightPercent,
                    Type = DiffType.New,
                    ChangePercent = null
                });
            }

            return new HoldingDiff
            {
                Id = Guid.NewGuid(),
                ComputedAt = DateTime.UtcNow,
                PreviousSnapshotId = previous.Id,
                CurrentSnapshotId = current.Id,
                Entries = entries
            };
        }
    }
}
