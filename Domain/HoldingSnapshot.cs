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

    public bool ContentEquals(HoldingSnapshot other)
    {
        if (other is null) return false;

        if (Holdings.Count != other.Holdings.Count)
            return false;

        var current = Holdings.ToDictionary(h => h.Cusip);
        var incoming = other.Holdings.ToDictionary(h => h.Cusip);

        if (current.Count != incoming.Count)
            return false;

        foreach (var (cusip, record) in current)
        {
            if (!incoming.TryGetValue(cusip, out var otherRecord))
                return false;

            if (!record.Equals(otherRecord))
                return false;
        }

        return true;
    }
}
