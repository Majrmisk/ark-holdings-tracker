using Domain;

namespace Presentation.Models;

public class TableViewModel
{
    public HoldingDiff? Diff { get; set; }
    public Colorscheme ColorScheme { get; set; } = new();
    public bool CanToggleColors { get; set; }
    public bool ColorsEnabled { get; set; } = true;
}