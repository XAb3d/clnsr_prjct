namespace CleanserBlazorUI.Entities;
public class CellDataAndStatus
{
    public string? Data { get; set; } = string.Empty;
    public string? OldData { get; set; } = string.Empty;
    public bool Passed { get; set; } = true;
    public List<string>? Errors { get; set; }
    public string? IsSameCustomerButDifferntId { get; set; } = string.Empty;
    public CellDataAndStatus(string data)
    {
       Errors = new List<string>();
       OldData = data;
    }
}
