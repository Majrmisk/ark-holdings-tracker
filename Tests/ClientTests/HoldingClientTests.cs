using Application.Clients;
using Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Tests.ClientTests
{
    public class SpyLogger<T> : ILogger<T>
    {
        private readonly List<(LogLevel Level, string Message, Exception? Ex)> _entries = new();

        public bool HasErrorOrAbove => _entries.Any(e => e.Level >= LogLevel.Error);
        public bool HasWarningOrAbove => _entries.Any(e => e.Level >= LogLevel.Warning);
        public IReadOnlyList<(LogLevel Level, string Message, Exception? Ex)> Entries => _entries;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
            => _entries.Add((logLevel, formatter(state, exception), exception));
    }

    public class HoldingClientTests : IDisposable
    {
        private readonly WireMockServer _server;
        private const string CsvPath = "/holdings.csv";

        private const string ValidCsvContent =
            "date,fund,company,ticker,cusip,shares,market value($),weight(%)\n" +
            "04/25/2025,ARKK,Tesla Inc,TSLA,88160R101,\"3,000,000\",\"$750,000,000\",\"10.50%\"\n" +
            "04/25/2025,ARKK,Coinbase Global Inc,COIN,19260Q107,\"1,500,000\",\"$300,000,000\",\"4.20%\"";

        public HoldingClientTests()
        {
            _server = WireMockServer.Start();
        }

        private (HoldingClient Client, SpyLogger<HoldingClient> Logger) CreateClient(
            TimeSpan? httpTimeout = null)
        {
            var logger = new SpyLogger<HoldingClient>();
            var options = Options.Create(new ArkFundClientSettings
            {
                CsvUrl = $"{_server.Url}{CsvPath}"
            });
            var httpClient = new HttpClient
            {
                Timeout = httpTimeout ?? TimeSpan.FromSeconds(10)
            };

            var client = new HoldingClient(httpClient, options, logger);
            return (client, logger);
        }

        [Fact]
        public async Task Fetch_ReturnsSnapshot_WhenCsvIsValid()
        {
            _server.Given(Request.Create().WithPath(CsvPath).UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200).WithBody(ValidCsvContent));

            var (client, _) = CreateClient();
            var result = await client.Fetch();

            Assert.NotNull(result);
            Assert.Equal(2, result.Holdings.Count);

            var tesla = result.Holdings[0];
            Assert.Equal("Tesla Inc", tesla.Company);
            Assert.Equal("TSLA", tesla.Ticker);
            Assert.Equal("88160R101", tesla.Cusip);
            Assert.Equal(3_000_000L, tesla.Shares);
            Assert.Equal(750_000_000m, tesla.MarketValueUsd);
            Assert.Equal(10.50m, tesla.WeightPercent);
        }

        [Fact]
        public async Task Fetch_AssignsNonEmptyId_WhenCsvIsValid()
        {
            _server.Given(Request.Create().WithPath(CsvPath).UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200).WithBody(ValidCsvContent));

            var (client, _) = CreateClient();
            var result = await client.Fetch();

            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
        }

        [Fact]
        public async Task Fetch_SetsFetchedAtToUtcNow_WhenCsvIsValid()
        {
            _server.Given(Request.Create().WithPath(CsvPath).UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200).WithBody(ValidCsvContent));

            var before = DateTime.UtcNow;
            var (client, _) = CreateClient();
            var result = await client.Fetch();
            var after = DateTime.UtcNow;

            Assert.NotNull(result);
            Assert.InRange(result.FetchedAt, before, after);
        }

        [Fact]
        public async Task Fetch_SkipsMalformedRows_AndReturnsValidOnes()
        {
            var csv =
                "date,fund,company,ticker,cusip,shares,market value($),weight(%)\n" +
                "bad,incomplete,row\n" +
                "04/25/2025,ARKK,Tesla Inc,TSLA,88160R101,\"3,000,000\",\"$750,000,000\",\"10.50%\"";

            _server.Given(Request.Create().WithPath(CsvPath).UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200).WithBody(csv));

            var (client, logger) = CreateClient();
            var result = await client.Fetch();

            Assert.NotNull(result);
            Assert.Single(result.Holdings);
            Assert.Equal("TSLA", result.Holdings[0].Ticker);
        }

        [Fact]
        public async Task Fetch_ReturnsNull_WhenCsvHasHeaderOnly()
        {
            _server.Given(Request.Create().WithPath(CsvPath).UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithBody("date,fund,company,ticker,cusip,shares,market value($),weight(%)"));

            var (client, logger) = CreateClient();
            var result = await client.Fetch();

            Assert.Null(result);
            Assert.True(!logger.HasWarningOrAbove,
                "Log was not expected after receiving header only csv.");
        }

        [Fact]
        public async Task Fetch_ReturnsNull_LogsWarning_WhenCsvIsInvalid()
        {
            _server.Given(Request.Create().WithPath(CsvPath).UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithBody("CoMpLeTeLy-InVaLiD_CsV{]!@?>\"\']"));

            var (client, logger) = CreateClient();
            var result = await client.Fetch();

            Assert.Null(result);
            Assert.True(logger.HasWarningOrAbove,
                "Expected a warning-level log entry when an invalid CSV is received.");
        }

        [Fact]
        public async Task Fetch_ReturnsNull_LogError_OnHttp404()
        {
            _server.Given(Request.Create().WithPath(CsvPath).UsingGet())
                .RespondWith(Response.Create().WithStatusCode(404));

            var (client, logger) = CreateClient();
            var result = await client.Fetch();

            Assert.Null(result);
            Assert.True(logger.HasErrorOrAbove,
                "Expected an error-level log entry when the server returns 404.");
        }

        [Fact]
        public async Task Fetch_ReturnsNull_LogError_OnHttp500()
        {
            _server.Given(Request.Create().WithPath(CsvPath).UsingGet())
                .RespondWith(Response.Create().WithStatusCode(500));

            var (client, logger) = CreateClient();
            var result = await client.Fetch();

            Assert.Null(result);
            Assert.True(logger.HasErrorOrAbove,
                "Expected an error-level log entry when the server returns 500.");
        }

        [Fact]
        public async Task Fetch_ReturnsNull_LogsError_OnTimeout()
        {
            _server.Given(Request.Create().WithPath(CsvPath).UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithDelay(TimeSpan.FromSeconds(30)));

            var (client, logger) = CreateClient(httpTimeout: TimeSpan.FromMilliseconds(200));
            var result = await client.Fetch();

            Assert.Null(result);
            Assert.True(logger.HasErrorOrAbove,
                "Expected an error-level log entry when the HTTP call times out.");
        }

        [Fact]
        public async Task Fetch_RetriesOnTransientError_AndSucceedsOnThirdAttempt()
        {
            // First two requests return 503, third returns the valid CSV.
            _server
                .Given(Request.Create().WithPath(CsvPath).UsingGet())
                .InScenario("retry-success")
                .WillSetStateTo("attempt-1")
                .RespondWith(Response.Create().WithStatusCode(503));

            _server
                .Given(Request.Create().WithPath(CsvPath).UsingGet())
                .InScenario("retry-success")
                .WhenStateIs("attempt-1")
                .WillSetStateTo("attempt-2")
                .RespondWith(Response.Create().WithStatusCode(503));

            _server
                .Given(Request.Create().WithPath(CsvPath).UsingGet())
                .InScenario("retry-success")
                .WhenStateIs("attempt-2")
                .RespondWith(Response.Create().WithStatusCode(200).WithBody(ValidCsvContent));

            var (client, _) = CreateClient();
            var result = await client.Fetch();

            Assert.NotNull(result);
            Assert.Equal(3, _server.LogEntries.Count());
        }

        [Fact]
        public async Task Fetch_ReturnsNull_LogsError_AfterExhaustingAllRetries()
        {
            _server.Given(Request.Create().WithPath(CsvPath).UsingGet())
                .RespondWith(Response.Create().WithStatusCode(503));

            var (client, logger) = CreateClient();
            var result = await client.Fetch();

            Assert.Null(result);
            Assert.True(_server.LogEntries.Count() == 3,
                $"Expected multiple attempts due to retry policy, but got {_server.LogEntries.Count()}.");
            Assert.True(logger.HasErrorOrAbove,
                "Expected an error-level log after all retries are exhausted.");
        }

        [Fact]
        public async Task Fetch_ReturnsNull_LogsError_OnHttpRequestException()
        {
            var logger = new SpyLogger<HoldingClient>();
            var options = Options.Create(new ArkFundClientSettings
            {
                CsvUrl = "http://localhost:1"   // nothing listening
            });
            var client = new HoldingClient(new HttpClient(), options, logger);

            var result = await client.Fetch();

            Assert.Null(result);
            Assert.True(logger.HasErrorOrAbove,
                "Expected an error-level log after HttpRequestException.");
        }

        [Fact]
        public async Task Fetch_AppliesExponentialBackoff_BetweenRetries()
        {
            _server.Given(Request.Create().WithPath(CsvPath).UsingGet())
                .RespondWith(Response.Create().WithStatusCode(503));

            var (client, _) = CreateClient();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            await client.Fetch();
            sw.Stop();

            Assert.True(sw.Elapsed > TimeSpan.FromSeconds(7),
                $"Expected back-off delays between retries, but Fetch completed in {sw.Elapsed.TotalMilliseconds:F0} ms.");
        }

        public void Dispose() => _server.Stop();
    }
}