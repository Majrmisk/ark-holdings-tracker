using System.Text.Json.Serialization;

namespace Domain;

public class HoldingSnapshot
{
    [JsonInclude]
    public Guid Id { get; private set; }

    [JsonInclude]
    public DateTimeOffset FetchedAt { get; private set; }

    [JsonInclude]
    public List<HoldingRecord> Holdings { get; private set; } = [];

    public HoldingSnapshot()
    {
    }

    public HoldingSnapshot(DateTimeOffset fetchedAt, IEnumerable<HoldingRecord> holdings)
        : this(Guid.NewGuid(), fetchedAt, holdings)
    {
    }

    public HoldingSnapshot(Guid id, DateTimeOffset fetchedAt, IEnumerable<HoldingRecord> holdings)
    {
        Id = id;
        FetchedAt = fetchedAt;
        Holdings = holdings.ToList();
    }
}
