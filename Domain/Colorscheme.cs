namespace Domain;

public class Colorscheme
{
    public string New { get; private set; } = string.Empty;
    public string Increased { get; private set; } = string.Empty;
    public string Reduced { get; private set; } = string.Empty;
    public string Removed { get; private set; } = string.Empty;
    public string Unchanged { get; private set; } = string.Empty;
    public string WeightShifted { get; private set; } = string.Empty;

    public Colorscheme()
    {
    }

    public Colorscheme(
        string newColor,
        string increased,
        string reduced,
        string removed,
        string unchanged,
        string weightShifted)
    {
        New = newColor;
        Increased = increased;
        Reduced = reduced;
        Removed = removed;
        Unchanged = unchanged;
        WeightShifted = weightShifted;
    }
}
