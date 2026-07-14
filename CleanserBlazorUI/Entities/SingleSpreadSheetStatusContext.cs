namespace CleanserBlazorUI.Entities;
public class SpreadSheetStatusContext
{
    public int TotolRow { get; set; } = default;
    public int TotolCol { get; set; } = default;
    public bool IsHeaderValid { get; set; } = default;
    public string HeaderStatusMessage { get; set; } = string.Empty;
    public List<ErrorsPairs> InvalidHeaders { get; set; } = new List<ErrorsPairs>();
}
public record ErrorsPairs(string ExpectedHeader, string UploadedHeader);
