using System.Text.Json.Serialization;

namespace Domain;

public class DiffEntry
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
    public decimal WeightPercent { get; private set; }

    [JsonInclude]
    public DiffType Type { get; private set; }

    [JsonInclude]
    public decimal? ChangePercent { get; private set; }

    public DiffEntry()
    {
    }

    public DiffEntry(
        string company,
        string? ticker,
        string cusip,
        long shares,
        decimal weightPercent,
        DiffType type,
        decimal? changePercent)
    {
        Company = company;
        Ticker = ticker;
        Cusip = cusip;
        Shares = shares;
        WeightPercent = weightPercent;
        Type = type;
        ChangePercent = changePercent;
    }
}
