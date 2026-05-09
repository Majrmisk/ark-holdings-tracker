using Application.Interfaces;
using Application.Settings;
using Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services
{
    public class HoldingService : IHoldingService
    {
        private readonly IHoldingsRepository _holdingsRepository;
        private readonly IHoldingClient _holdingClient;
        private readonly ILogger<HoldingService> _logger;
        private readonly int _deleteAfterDays;

        public HoldingService(
            IHoldingsRepository holdingsRepository,
            IHoldingClient holdingClient,
            IOptions<DataRetentionSettings> options,
            ILogger<HoldingService> logger)
        {
            _holdingsRepository = holdingsRepository;
            _holdingClient = holdingClient;
            _logger = logger;
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
            {
                _logger.LogWarning("Holding fetch returned no snapshot. Data was not updated.");
                return;
            }

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
                        entries.Add(new DiffEntry(
                            curr.Company,
                            curr.Ticker,
                            cusip,
                            curr.Shares,
                            curr.WeightPercent,
                            curr.WeightPercent != prev.WeightPercent ? DiffType.WeightShifted : DiffType.Unchanged,
                            0m));
                        continue;
                    }

                    var changePercent = (decimal)(curr.Shares - prev.Shares) / prev.Shares * 100;

                    entries.Add(new DiffEntry(
                        curr.Company,
                        curr.Ticker,
                        cusip,
                        curr.Shares,
                        curr.WeightPercent,
                        curr.Shares > prev.Shares ? DiffType.Increased : DiffType.Reduced,
                        changePercent));
                }
                else
                {
                    entries.Add(new DiffEntry(
                        prev.Company,
                        prev.Ticker,
                        cusip,
                        0,
                        0,
                        DiffType.Removed,
                        -100m));
                }
            }

            foreach (var (cusip, curr) in currentMap.Where(c => !previousMap.ContainsKey(c.Key)))
            {
                entries.Add(new DiffEntry(
                    curr.Company,
                    curr.Ticker,
                    cusip,
                    curr.Shares,
                    curr.WeightPercent,
                    DiffType.New,
                    null));
            }

            return new HoldingDiff(previous.Id, current.Id, entries);
        }
    }
}
