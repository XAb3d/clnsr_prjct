namespace CleanserBlazorUI.Entities;

/// <summary>
/// In-memory only (never mapped by EF): what a record looked like at the one point where
/// the name-review feature can capture it correctly -- right after the file is read and any
/// reviewer correction has been applied, before ISCleaned.
///
/// Why a snapshot instead of reading the record later: ISCleaned replaces the name cells with
/// freshly built ones, so by the time the Name Review screen runs, CellDataAndStatus.OldData
/// is no longer the cell as submitted. Raw text captured from there would never match the
/// next run's raw text, and a saved correction would silently stop applying.
/// </summary>
public sealed class NameReviewSnapshot
{
    /// <summary>The cleaner's normalized CustomerID.</summary>
    public string CustomerKey { get; set; } = string.Empty;

    // Names exactly as submitted (CellDataAndStatus.OldData at read time).
    public string RawSurname { get; set; } = string.Empty;
    public string RawFirstName { get; set; } = string.Empty;
    public string RawMiddleNames { get; set; } = string.Empty;
    public string RawBusinessName { get; set; } = string.Empty;

    /// <summary>NameReviewText.NormalizeDob ("" for businesses / unknown).</summary>
    public string Dob { get; set; } = string.Empty;

    /// <summary>NameReviewText.BuildAnchorIds.</summary>
    public string AnchorIds { get; set; } = string.Empty;
}
