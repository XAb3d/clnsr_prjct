using Microsoft.AspNetCore.Components.Forms;

namespace CleanserBlazorUI.Entities;
public class SpreadsheetFileAndChipColor
{
    public IBrowserFile? SpreadsheetFile { get; set; }
    public MudBlazor.Color SpreadsheetFileColor { get; set; }
    public bool? IsReferenceAvailable { get; set; }
    public bool IsPassed { get; set; }
}