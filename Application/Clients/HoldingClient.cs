using Application.Interfaces;
using Domain;
using System.Globalization;
using System.Text;

namespace Application.Clients
{
    public class HoldingClient : IHoldingClient
    {
        private readonly static Uri BASE_URL = new Uri("https://assets.ark-funds.com/fund-documents/funds-etf-csv/ARK_INNOVATION_ETF_ARKK_HOLDINGS.csv");
        private readonly HttpClient _http;

        public HoldingClient(HttpClient httpClient)
        {
            _http = httpClient;
        }

        public async Task<HoldingSnapshot?> Fetch()
        {
            var response = await _http.GetAsync(BASE_URL);

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 2)
                return null;

            var holdings = new List<HoldingRecord>();
            var fetchedAt = DateTime.UtcNow;

            foreach (var line in lines.Skip(1))
            {
                var fields = ParseCsvLine(line);

                if (fields.Length < 8)
                    continue;

                long.TryParse(Sanitize(fields[5]), out var shares);
                decimal.TryParse(Sanitize(fields[6]), NumberStyles.Any, CultureInfo.InvariantCulture, out var marketValue);
                decimal.TryParse(Sanitize(fields[7]), NumberStyles.Any, CultureInfo.InvariantCulture, out var weight);

                holdings.Add(new HoldingRecord
                {
                    Company = fields[2].Trim(),
                    Ticker = fields[3].Trim(),
                    Cusip = fields[4].Trim(),
                    Shares = shares,
                    MarketValueUsd = marketValue,
                    WeightPercent = weight
                });
            }

            return new HoldingSnapshot
            {
                Id = Guid.NewGuid(),
                FetchedAt = fetchedAt,
                Holdings = holdings
            };
        }

        private static string Sanitize(string field) =>
            field.Replace("$", "").Replace("%", "").Replace(",", "").Trim();

        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            // Parsing by char, bcs report has 
            // numbers with "," - "7,922,076"
            foreach (char c in line)
            {
                if (c == '"')
                    inQuotes = !inQuotes;
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                    current.Append(c);
            }

            fields.Add(current.ToString());
            return fields.ToArray();
        }
    }
}
