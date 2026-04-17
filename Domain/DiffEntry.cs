namespace Domain;

public class DiffEntry
{
    public string Company { get; set; } = string.Empty;
    public string? Ticker { get; set; }
    public string Cusip { get; set; } = string.Empty;
    public long Shares { get; set; }
    public decimal WeightPercent { get; set; }
    public DiffType Type { get; set; }
    public decimal? ChangePercent { get; set; }
}
