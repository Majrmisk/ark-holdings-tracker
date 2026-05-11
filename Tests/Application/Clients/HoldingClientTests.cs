using Application.Clients;
using Application.Settings;
using Microsoft.Extensions.Options;
using Shouldly;
using Tests.TestHelpers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Tests.Application.Clients;

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

        result.ShouldNotBeNull();
        result.Holdings.Count.ShouldBe(2);

        var tesla = result.Holdings[0];
        tesla.Company.ShouldBe("Tesla Inc");
        tesla.Ticker.ShouldBe("TSLA");
        tesla.Cusip.ShouldBe("88160R101");
        tesla.Shares.ShouldBe(3_000_000L);
        tesla.MarketValueUsd.ShouldBe(750_000_000m);
        tesla.WeightPercent.ShouldBe(10.50m);
    }

    [Fact]
    public async Task Fetch_AssignsNonEmptyId_WhenCsvIsValid()
    {
        _server.Given(Request.Create().WithPath(CsvPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(ValidCsvContent));

        var (client, _) = CreateClient();
        var result = await client.Fetch();

        result.ShouldNotBeNull();
        result.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Fetch_SetsFetchedAtToUtcNow_WhenCsvIsValid()
    {
        _server.Given(Request.Create().WithPath(CsvPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(ValidCsvContent));

        var before = DateTimeOffset.UtcNow;
        var (client, _) = CreateClient();
        var result = await client.Fetch();
        var after = DateTimeOffset.UtcNow;

        result.ShouldNotBeNull();
        result.FetchedAt.ShouldBeGreaterThanOrEqualTo(before);
        result.FetchedAt.ShouldBeLessThanOrEqualTo(after);
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

        result.ShouldNotBeNull();
        result.Holdings.Count.ShouldBe(1);
        result.Holdings[0].Ticker.ShouldBe("TSLA");
    }

    [Fact]
    public async Task Fetch_ReturnsNull_WhenCsvHasHeaderOnly()
    {
        _server.Given(Request.Create().WithPath(CsvPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("date,fund,company,ticker,cusip,shares,market value($),weight(%)"));

        var (client, logger) = CreateClient();
        var result = await client.Fetch();

        result.ShouldBeNull();
        logger.HasWarningOrAbove.ShouldBeFalse(
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

        result.ShouldBeNull();
        logger.HasWarningOrAbove.ShouldBeTrue(
            "Expected a warning-level log entry when an invalid CSV is received.");
    }

    [Fact]
    public async Task Fetch_ReturnsNull_LogError_OnHttp404()
    {
        _server.Given(Request.Create().WithPath(CsvPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

        var (client, logger) = CreateClient();
        var result = await client.Fetch();

        result.ShouldBeNull();
        logger.HasErrorOrAbove.ShouldBeTrue(
            "Expected an error-level log entry when the server returns 404.");
    }

    [Fact]
    public async Task Fetch_ReturnsNull_LogError_OnHttp500()
    {
        _server.Given(Request.Create().WithPath(CsvPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500));

        var (client, logger) = CreateClient();
        var result = await client.Fetch();

        result.ShouldBeNull();
        logger.HasErrorOrAbove.ShouldBeTrue(
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

        result.ShouldBeNull();
        logger.HasErrorOrAbove.ShouldBeTrue(
            "Expected an error-level log entry when the HTTP call times out.");
    }

    [Fact]
    public async Task Fetch_RetriesOnTransientError_AndSucceedsOnThirdAttempt()
    {
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

        result.ShouldNotBeNull();
        _server.LogEntries.Count().ShouldBe(3);
    }

    [Fact]
    public async Task Fetch_ReturnsNull_LogsError_AfterExhaustingAllRetries()
    {
        _server.Given(Request.Create().WithPath(CsvPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503));

        var (client, logger) = CreateClient();
        var result = await client.Fetch();

        result.ShouldBeNull();
        _server.LogEntries.Count().ShouldBe(3,
            $"Expected multiple attempts due to retry policy, but got {_server.LogEntries.Count()}.");
        logger.HasErrorOrAbove.ShouldBeTrue(
            "Expected an error-level log after all retries are exhausted.");
    }

    [Fact]
    public async Task Fetch_ReturnsNull_LogsError_OnHttpRequestException()
    {
        var logger = new SpyLogger<HoldingClient>();
        var options = Options.Create(new ArkFundClientSettings
        {
            CsvUrl = "http://localhost:1"
        });
        var client = new HoldingClient(new HttpClient(), options, logger);

        var result = await client.Fetch();

        result.ShouldBeNull();
        logger.HasErrorOrAbove.ShouldBeTrue(
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

        (sw.Elapsed > TimeSpan.FromSeconds(7)).ShouldBeTrue(
            $"Expected back-off delays between retries, but Fetch completed in {sw.Elapsed.TotalMilliseconds:F0} ms.");
    }

    public void Dispose() => _server.Stop();
}
