namespace Domain;

public class HoldingDiff
{
    public Guid Id { get; set; }
    public DateTime ComputedAt { get; set; }
    public Guid PreviousSnapshotId { get; set; }
    public Guid CurrentSnapshotId { get; set; }
    public List<DiffEntry> Entries { get; set; } = [];
}
