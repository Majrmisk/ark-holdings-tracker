using Application.Interfaces;
using Application.Settings;
using Domain;
using Domain.Services;
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

            if (latest != null && latest.ContentEquals(snapshot))
            {
                _logger.LogInformation("Snapshot unchanged. Skipping persistence.");
                return;
            }

            if (latest != null)
            {
                var diff = HoldingDiffCalculator.Compute(latest, snapshot);
                await _holdingsRepository.SaveDiffAsync(diff);
            }

            await _holdingsRepository.SaveSnapshotAsync(snapshot);
        }
    }
}
