using Domain;

namespace App.Models;

public class TableViewModel
{
    public HoldingDiff? Diff { get; set; }
    public Colorscheme ColorScheme { get; set; } = new();
    public bool CanToggleColors { get; set; }
    public bool ColorsEnabled { get; set; } = true;
}