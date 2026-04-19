using Domain;

namespace App.Models;

public class TableViewModel
{
    public List<HoldingDiff> Diffs { get; set; }
    public Colorscheme ColorScheme { get; set; }
}