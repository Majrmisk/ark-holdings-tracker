using Application.Interfaces;
using Application.Settings;
using CsvHelper;
using CsvHelper.Configuration;
using Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;

namespace Application.Clients
{
    public class HoldingClient : IHoldingClient
    {
        private readonly Uri _baseUrl;
        private readonly HttpClient _http;
        private readonly ILogger<HoldingClient> _logger;
        private readonly int _fetchRetryCount = 3;
        private readonly int _exponentialBackoffBase = 2;

        public HoldingClient(HttpClient httpClient, IOptions<ArkFundClientSettings> options, ILogger<HoldingClient> logger)
        {
            _baseUrl = new Uri(options.Value.CsvUrl);
            _http = httpClient;
            _logger = logger;
        }

        public async Task<HoldingSnapshot?> Fetch()
        {
            HttpResponseMessage? response = await GetRequest();
            if (response == null || !response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
            };

            using var reader = new StringReader(content);
            using var csv = new CsvReader(reader, config);

            if (!csv.Read() || !csv.ReadHeader() ||
                csv.HeaderRecord == null || csv.HeaderRecord.Length < 8)
            {
                _logger.LogWarning("HoldingClient: Invalid CSV received");
                return null;
            }

            var holdings = new List<HoldingRecord>();
            var fetchedAt = DateTime.UtcNow;

            while (csv.Read())
            {
                if (csv.Parser.Count < 8)
                    continue;

                long.TryParse(Sanitize(csv.GetField(5) ?? ""), out var shares);
                decimal.TryParse(Sanitize(csv.GetField(6) ?? ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var marketValue);
                decimal.TryParse(Sanitize(csv.GetField(7) ?? ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var weight);

                holdings.Add(new HoldingRecord
                {
                    Company = (csv.GetField(2) ?? "").Trim(),
                    Ticker = (csv.GetField(3) ?? "").Trim(),
                    Cusip = (csv.GetField(4) ?? "").Trim(),
                    Shares = shares,
                    MarketValueUsd = marketValue,
                    WeightPercent = weight
                });
            }

            return holdings.Count == 0 ? null : new HoldingSnapshot
            {
                Id = Guid.NewGuid(),
                FetchedAt = fetchedAt,
                Holdings = holdings
            };
        }

        private async Task<HttpResponseMessage?> GetRequest()
        {
            HttpResponseMessage? response = null;
            for (int i = 0; i < _fetchRetryCount; i++)
            {
                try
                {
                    response = await _http.GetAsync(_baseUrl);
                }
                catch (UriFormatException)
                {
                    _logger.LogError("HoldingClient: Invalid URL");
                }
                catch (InvalidOperationException)
                {
                    _logger.LogError("HoldingClient: Invalid URL");
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError($"HoldingClient: Error while initiating connection: {ex.HttpRequestError}");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogError($"HoldingClient: Timeout | {i + 1}/{_fetchRetryCount} tries");
                }
                finally
                {
                    Thread.Sleep((int)Math.Pow(_exponentialBackoffBase, i) * 1000);
                }

                if (response == null)
                    continue;

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogError("HoldingClient: Received 404 status code");
                    break; // Don't retry, nothing would change
                }

                if ((int)response.StatusCode >= 500)
                    _logger.LogError($"HoldingClient: Received status code: {response.StatusCode}");
            }

            return response;
        }

        private static string Sanitize(string field) =>
            field.Replace("$", "").Replace("%", "").Replace(",", "").Trim();
    }
}
