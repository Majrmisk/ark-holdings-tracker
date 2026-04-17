namespace Domain;

public class HoldingSnapshot
{
    public Guid Id { get; set; }
    public DateTime FetchedAt { get; set; }
    public List<HoldingRecord> Holdings { get; set; } = [];
}
