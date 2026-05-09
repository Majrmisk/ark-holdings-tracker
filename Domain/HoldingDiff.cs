using System.Text.Json.Serialization;

namespace Domain;

public class HoldingDiff
{
    [JsonInclude]
    public Guid Id { get; private set; }

    [JsonInclude]
    public DateTimeOffset ComputedAt { get; private set; }

    [JsonInclude]
    public Guid PreviousSnapshotId { get; private set; }

    [JsonInclude]
    public Guid CurrentSnapshotId { get; private set; }

    [JsonInclude]
    public List<DiffEntry> Entries { get; private set; } = [];

    public HoldingDiff()
    {
    }

    public HoldingDiff(Guid previousSnapshotId, Guid currentSnapshotId, IEnumerable<DiffEntry> entries)
        : this(Guid.NewGuid(), DateTime.UtcNow, previousSnapshotId, currentSnapshotId, entries)
    {
    }

    public HoldingDiff(
        Guid id,
        DateTimeOffset computedAt,
        Guid previousSnapshotId,
        Guid currentSnapshotId,
        IEnumerable<DiffEntry> entries)
    {
        Id = id;
        ComputedAt = computedAt;
        PreviousSnapshotId = previousSnapshotId;
        CurrentSnapshotId = currentSnapshotId;
        Entries = entries.ToList();
    }
}
