namespace CleanserBlazorUI.Engine;

public sealed record RowEval(int RowIndex, bool IsUnl, string UnlReasons);

public sealed class EvaluateResponse
{
    public string RecordType { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public int UnlCount { get; set; }
    public List<RowEval> Rows { get; set; } = new();
}
