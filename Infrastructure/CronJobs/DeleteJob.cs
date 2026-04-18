using Application.Interfaces;
using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.CronJobs
{
    public class DeleteJob : BackgroundService
    {
        // Every day at 02:00 UTC
        private static readonly CronExpression Schedule =
            CronExpression.Parse("0 2 * * *", CronFormat.Standard);

        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public DeleteJob(ILogger<DeleteJob> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var next = Schedule.GetNextOccurrence(now, TimeZoneInfo.Utc);

                if (next is null) break;

                var delay = next.Value - now;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, ct);

                using var scope = _scopeFactory.CreateScope();
                var holdingService = scope.ServiceProvider.GetRequiredService<IHoldingService>();
                _logger.LogInformation("Running delete job at {Time}", DateTime.UtcNow);
                await holdingService.DeleteOldDataAsync();
            }
        }
    }
}
