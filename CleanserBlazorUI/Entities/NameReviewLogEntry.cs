using System.ComponentModel.DataAnnotations;

namespace CleanserBlazorUI.Entities;

/// <summary>
/// One row per reviewer decision on a name that was flagged unloadable.
///
/// A name can be flagged for reasons that are judgement calls (a business
/// keyword inside a genuine surname, an abbreviated business name, a
/// special character that is really a typo). A registrar reviews those on the
/// Name Review screen: fixes the text if needed (a correction), then approves
/// or rejects. This table is the record of that.
///
/// How it is used by the cleaning run:
///  - Corrections are applied automatically before validation on later runs,
///    so a subscriber that keeps sending "SMITH#" is fixed once, not monthly.
///  - Approval makes the (corrected) name trusted again for the same person:
///    same subscriber + same CustomerID (or, with matching DOB/ID anchors, a
///    different CustomerID) + matching name. Identity conflicts -- one
///    CustomerID linked to different people, ID/DOB mismatches -- are NOT
///    bypassed by approval; those checks always run.
///  - The trust check reads the LATEST decision for a given subscriber /
///    customer / name and treats Rejected or Revoked as a veto, even if the
///    reference data still holds the name. (Reference rows are never deleted
///    to "undo" an approval -- dedup and DOB history depend on them.)
///
/// History is kept: a revoked approval stays as a row (revoking either updates the
/// row's Status or adds a new Revoked row -- the trust check handles both), and a
/// later re-approval adds a new one. That is why the lookup index is deliberately
/// not unique, and why the LATEST decision is the one that counts.
/// </summary>
public class NameReviewLogEntry
{
    public int Id { get; set; }

    // ── Scope ────────────────────────────────────────────────────────────
    /// <summary>"IND" or "BUS" -- see <see cref="NameReviewFileKind"/>.</summary>
    [MaxLength(3)]
    public string FileKind { get; set; } = string.Empty;

    /// <summary>
    /// Short code derived from the filename (same value as
    /// SubscriberProfile.SubscriberCode / IndividualRef.SubscriberCode).
    /// Trust never crosses institutions.
    /// </summary>
    [MaxLength(50)]
    public string SubscriberCode { get; set; } = string.Empty;

    /// <summary>Normalized CustomerID the flagged record carried.</summary>
    [MaxLength(100)]
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Why the name was in the review queue; '|'-delimited values from
    /// <see cref="NameReviewFlagType"/>.
    /// </summary>
    [MaxLength(200)]
    public string FlagTypes { get; set; } = string.Empty;

    // ── Name as submitted (raw) ──────────────────────────────────────────
    // Individuals use the three part columns, businesses use RawBusinessName.
    // Unused columns stay empty.
    [MaxLength(200)] public string RawSurname { get; set; } = string.Empty;
    [MaxLength(200)] public string RawFirstName { get; set; } = string.Empty;
    [MaxLength(200)] public string RawMiddleNames { get; set; } = string.Empty;
    [MaxLength(400)] public string RawBusinessName { get; set; } = string.Empty;

    // ── Name after review (identical to raw when approved as-is) ─────────
    // Applied automatically on later runs: raw -> corrected, before validation.
    [MaxLength(200)] public string CorrectedSurname { get; set; } = string.Empty;
    [MaxLength(200)] public string CorrectedFirstName { get; set; } = string.Empty;
    [MaxLength(200)] public string CorrectedMiddleNames { get; set; } = string.Empty;
    [MaxLength(400)] public string CorrectedBusinessName { get; set; } = string.Empty;

    // ── Identity anchors captured at decision time ───────────────────────
    // Used to recognise "the same person" on a different CustomerID: name,
    // DOB and at least one valid ID must agree, with no contradicting ID.
    /// <summary>Individuals only. yyyyMMdd as submitted, or empty.</summary>
    [MaxLength(8)]
    public string AnchorDob { get; set; } = string.Empty;

    /// <summary>
    /// '|'-delimited normalized valid ID values: the nine individual ID
    /// columns for IND, Busregnum and Tinum for BUS. Placeholder-style values
    /// are excluded when captured.
    /// </summary>
    [MaxLength(1000)]
    public string AnchorIds { get; set; } = string.Empty;

    // ── Decision ─────────────────────────────────────────────────────────
    /// <summary>See <see cref="NameReviewStatus"/>.</summary>
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(256)]
    public string DecidedBy { get; set; } = string.Empty;
    public DateTime DecidedAtUtc { get; set; }

    /// <summary>Set only when Status becomes Revoked.</summary>
    [MaxLength(256)]
    public string? RevokedBy { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    [MaxLength(500)]
    public string Note { get; set; } = string.Empty;
}

public static class NameReviewStatus
{
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Revoked = "Revoked";
}

public static class NameReviewFileKind
{
    public const string Individual = "IND";
    public const string Business = "BUS";
}

/// <summary>The reviewable name flags. Stored '|'-delimited in FlagTypes.</summary>
public static class NameReviewFlagType
{
    public const string Keyword = "Keyword";                   // IND: business keyword in an individual's name
    public const string RestrictedWord = "RestrictedWord";     // IND: surname/first name hit the restricted-word list
    public const string InvalidBusinessName = "InvalidBusinessName"; // BUS: name has no recognised business keyword
    public const string SpecialCharacter = "SpecialCharacter"; // IND + BUS: characters the loader rejects (corrected, then approved)
}
