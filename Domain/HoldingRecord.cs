using System.Text.Json.Serialization;

namespace Domain;

public class HoldingRecord
{
    [JsonInclude]
    public string Company { get; private set; } = string.Empty;

    [JsonInclude]
    public string? Ticker { get; private set; }

    [JsonInclude]
    public string Cusip { get; private set; } = string.Empty;

    [JsonInclude]
    public long Shares { get; private set; }

    [JsonInclude]
    public decimal MarketValueUsd { get; private set; }

    [JsonInclude]
    public decimal WeightPercent { get; private set; }

    public HoldingRecord()
    {
    }

    public HoldingRecord(
        string company,
        string? ticker,
        string cusip,
        long shares,
        decimal marketValueUsd,
        decimal weightPercent)
    {
        Company = company;
        Ticker = ticker;
        Cusip = cusip;
        Shares = shares;
        MarketValueUsd = marketValueUsd;
        WeightPercent = weightPercent;
    }
}
