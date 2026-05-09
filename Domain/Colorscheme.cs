namespace Domain;

public class Colorscheme
{
    public string New { get; private set; } = "bg-green-500";
    public string Increased { get; private set; } = "bg-green-800";
    public string Reduced { get; private set; } = "bg-red-800";
    public string Removed { get; private set; } = "bg-yellow-500";
    public string Unchanged { get; private set; } = "bg-grey-500";
    public string WeightShifted { get; private set; } = "bg-purple-500";

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
