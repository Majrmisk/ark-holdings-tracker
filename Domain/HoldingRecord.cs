namespace Domain;

public class HoldingRecord
{
    public string Company { get; set; } = string.Empty;
    public string? Ticker { get; set; }
    public string Cusip { get; set; } = string.Empty;
    public long Shares { get; set; }
    public decimal MarketValueUsd { get; set; }
    public decimal WeightPercent { get; set; }
}
