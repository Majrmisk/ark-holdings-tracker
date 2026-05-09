namespace Domain.Services;

public static class HoldingDiffCalculator
{
    public static HoldingDiff Compute(HoldingSnapshot previous, HoldingSnapshot current)
    {
        var entries = new List<DiffEntry>();

        var previousMap = previous.Holdings.ToDictionary(h => h.Cusip);
        var currentMap = current.Holdings.ToDictionary(h => h.Cusip);

        foreach (var (cusip, prev) in previousMap)
        {
            if (currentMap.TryGetValue(cusip, out var curr))
            {
                if (curr.Shares == prev.Shares)
                {
                    entries.Add(new DiffEntry(
                        curr.Company,
                        curr.Ticker,
                        cusip,
                        curr.Shares,
                        curr.WeightPercent,
                        curr.WeightPercent != prev.WeightPercent ? DiffType.WeightShifted : DiffType.Unchanged,
                        0m));
                    continue;
                }

                var changePercent = (decimal)(curr.Shares - prev.Shares) / prev.Shares * 100;

                entries.Add(new DiffEntry(
                    curr.Company,
                    curr.Ticker,
                    cusip,
                    curr.Shares,
                    curr.WeightPercent,
                    curr.Shares > prev.Shares ? DiffType.Increased : DiffType.Reduced,
                    changePercent));
            }
            else
            {
                entries.Add(new DiffEntry(
                    prev.Company,
                    prev.Ticker,
                    cusip,
                    0,
                    0,
                    DiffType.Removed,
                    -100m));
            }
        }

        foreach (var (cusip, curr) in currentMap.Where(c => !previousMap.ContainsKey(c.Key)))
        {
            entries.Add(new DiffEntry(
                curr.Company,
                curr.Ticker,
                cusip,
                curr.Shares,
                curr.WeightPercent,
                DiffType.New,
                null));
        }

        return new HoldingDiff(previous.Id, current.Id, entries);
    }
}
