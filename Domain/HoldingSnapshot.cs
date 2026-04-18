namespace Domain;

public class HoldingSnapshot
{
    public Guid Id { get; set; }
    public DateTimeOffset FetchedAt { get; set; }
    public List<HoldingRecord> Holdings { get; set; } = [];
}
