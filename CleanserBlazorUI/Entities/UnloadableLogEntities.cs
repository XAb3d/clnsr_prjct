using System.ComponentModel.DataAnnotations.Schema;

namespace CleanserBlazorUI.Entities;

/// <summary>
/// One row per Unloadable Log run -- the persisted, editable counterpart to
/// the header row in UnloadableLogService.GenerateWorkbook. The .xlsx is
/// still generated and downloaded exactly as before; this is written
/// alongside it so the log becomes a queryable running history across all
/// 200+ subscribers instead of a folder of one-off spreadsheets.
///
/// FK's to SubscriberProfile rather than duplicating SubscriberCode/Name/
/// InstitutionType here -- those already live in one place (kept current by
/// SaveSubscriberProfileAsync) and this avoids the same institution's name
/// drifting across hundreds of header rows if it's ever corrected.
///
/// SerialNo from the old in-memory _unlLogSerialCounter is intentionally
/// dropped: that counter reset on every app restart and was never a stable
/// identifier. Id is now the real, DB-generated ordinal.
/// </summary>
public class UnloadableLogHeader
{
    public int Id { get; set; }

    public int SubscriberProfileId { get; set; }
    [ForeignKey(nameof(SubscriberProfileId))]
    public SubscriberProfile? SubscriberProfile { get; set; }

    public string Associate { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public int NumberOfRecords { get; set; }
    public string ReportingPeriod { get; set; } = string.Empty;
    public string ReportingYear { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string Months { get; set; } = string.Empty;
    public string LogYear { get; set; } = string.Empty;

    // ── Editable after generation, via the log grid (not set at insert time) ──
    public DateTime? DateEmailed { get; set; }
    public DateTime? DateFixed { get; set; }
    public string? Comments { get; set; }

    // ── Immutable: when the run actually happened ──────────────────────────
    public DateTime CreatedDate { get; set; }

    public List<UnloadableLogMessageDetail> MessageDetails { get; set; } = new();
    public List<UnloadableLogCategoryDetail> CategoryDetails { get; set; } = new();
}

/// <summary>
/// Table 1 from the workbook (error-message breakdown), one row per distinct
/// message per run.
/// </summary>
public class UnloadableLogMessageDetail
{
    public int Id { get; set; }

    public int UnloadableLogHeaderId { get; set; }
    [ForeignKey(nameof(UnloadableLogHeaderId))]
    public UnloadableLogHeader? UnloadableLogHeader { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Table 2 from the workbook (category rollup), one row per subcategory per
/// run. TopLevelCategory is "Demographic" / "Financial" / "FacilitySubmission"
/// matching UnloadableLogService's MessageCategoryRules.
/// </summary>
public class UnloadableLogCategoryDetail
{
    public int Id { get; set; }

    public int UnloadableLogHeaderId { get; set; }
    [ForeignKey(nameof(UnloadableLogHeaderId))]
    public UnloadableLogHeader? UnloadableLogHeader { get; set; }

    public string TopLevelCategory { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
    public string DescriptionOfErrors { get; set; } = string.Empty;
    public int VolumeAffected { get; set; }
    public double Percentage { get; set; }
}
