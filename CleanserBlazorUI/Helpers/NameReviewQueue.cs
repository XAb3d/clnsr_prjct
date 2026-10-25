using CleanserBlazorUI.Entities;

namespace CleanserBlazorUI.Helpers;

/// <summary>One cell of a record after ISCleaned: its pass flag and every error message on it.</summary>
public sealed record CellState(string Cell, bool Passed, IReadOnlyList<string> Errors);

/// <summary>
/// Direct tests of the name, run by the caller with the cleaner's own helpers. Needed because
/// some messages are ambiguous: "SURNAME AND FIRSTNAME ARE MANDATORY..." is raised both for a
/// restricted-word hit (reviewable) and for a surname that is simply blank (not reviewable).
/// </summary>
public sealed record NameProbe(bool SpecialCharacter = false, bool RestrictedWord = false, bool MultipleIndividuals = false);

public sealed record NameReviewVerdict(bool Reviewable, IReadOnlyList<string> Flags, string WhyNot);

/// <summary>
/// Decides whether an unloadable record is UNL ONLY because of name flags a registrar can
/// judge. Anything else -- another error, a failure with no message, a missing name, two
/// individuals on one facility, a trading-name error -- keeps it out of the queue.
/// </summary>
public static class NameReviewClassifier
{
    private static readonly HashSet<string> IndividualNameCells = new(StringComparer.OrdinalIgnoreCase) { "Surname", "FirstName", "MiddleNames" };
    private static readonly HashSet<string> BusinessNameCells = new(StringComparer.OrdinalIgnoreCase) { "Businessname" };

    private enum Msg { Other, Keyword, Mandatory, Special, Multiple, InvalidBusinessName, BusinessChars }

    private static Msg Identify(string message, bool business)
    {
        var m = message.ToUpperInvariant();
        if (business)
        {
            if (m.Contains("INVALID BUSINESS NAME")) return Msg.InvalidBusinessName;
            if (m.Contains("BUSINESSNAME: INVALID CHARACTERS")) return Msg.BusinessChars;
            // Includes "Tradingname: INVALID CHARACTERS": the log has no trading-name columns to record a correction.
            return Msg.Other;
        }
        if (m.Contains("BUSINESS KEYWORDS DETECTED")) return Msg.Keyword;
        if (m.Contains("MULTIPLE INDIVIDUALS DETECTED")) return Msg.Multiple;
        // The source message spells it "SEPECIAL"; accept the correct spelling too in case it gets fixed.
        if (m.Contains("CANNOT CONTAIN SEPECIAL CHARACTER") || m.Contains("CANNOT CONTAIN SPECIAL CHARACTER")) return Msg.Special;
        if (m.Contains("ARE MANDATORY AND SHOULD CONTAIN VALID NAMES")) return Msg.Mandatory;
        return Msg.Other;
    }

    private static NameReviewVerdict No(string why) => new(false, Array.Empty<string>(), why);

    public static NameReviewVerdict Classify(string fileKind, IEnumerable<CellState> cells, NameProbe probe)
    {
        var business = string.Equals(fileKind, NameReviewFileKind.Business, StringComparison.OrdinalIgnoreCase);
        var nameCells = business ? BusinessNameCells : IndividualNameCells;
        var seen = new HashSet<Msg>();

        foreach (var cell in cells)
        {
            var messages = (cell.Errors ?? Array.Empty<string>()).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
            // A non-name cell that failed without saying why is an unknown reason -- never assume it is the name.
            if (!cell.Passed && messages.Count == 0 && !nameCells.Contains(cell.Cell))
                return No($"{cell.Cell} failed without a message");
            foreach (var message in messages)
            {
                var id = Identify(message, business);
                if (id == Msg.Other) return No($"other error: {message.Trim()}");
                seen.Add(id);
            }
        }

        if (seen.Count == 0) return No("no name error found");
        if (seen.Contains(Msg.Multiple) || probe.MultipleIndividuals) return No("more than one individual on one facility");
        if (seen.Contains(Msg.Mandatory) && !probe.RestrictedWord) return No("surname or first name is missing or invalid");

        var flags = new List<string>();
        if (seen.Contains(Msg.Keyword)) flags.Add(NameReviewFlagType.Keyword);
        if (seen.Contains(Msg.Mandatory)) flags.Add(NameReviewFlagType.RestrictedWord);
        if (seen.Contains(Msg.InvalidBusinessName)) flags.Add(NameReviewFlagType.InvalidBusinessName);
        // For a business, ISCleaned overwrites the transformer's invalid-characters message with
        // "Invalid Business Name" when the keyword test also fails, so the character problem can be
        // hidden; the direct probe puts it back.
        if (seen.Contains(Msg.Special) || seen.Contains(Msg.BusinessChars) || (business && probe.SpecialCharacter && seen.Contains(Msg.InvalidBusinessName)))
            flags.Add(NameReviewFlagType.SpecialCharacter);

        return flags.Count == 0 ? No("no reviewable name flag") : new NameReviewVerdict(true, flags, string.Empty);
    }
}

/// <summary>One row in the review queue: a customer + exact raw name, however many records carry it.</summary>
public sealed class NameReviewItem
{
    public string FileKind { get; init; } = string.Empty;
    public string CustomerKey { get; init; } = string.Empty;
    public string RawSurname { get; init; } = string.Empty;
    public string RawFirstName { get; init; } = string.Empty;
    public string RawMiddleNames { get; init; } = string.Empty;
    public string RawBusinessName { get; init; } = string.Empty;
    public List<string> Flags { get; } = new();
    public string AnchorDob { get; set; } = string.Empty;
    public string AnchorIds { get; set; } = string.Empty;
    public int RecordCount { get; set; }

    public bool IsBusiness => string.Equals(FileKind, NameReviewFileKind.Business, StringComparison.OrdinalIgnoreCase);
    public string DisplayName => IsBusiness ? RawBusinessName : $"{RawSurname} {RawFirstName} {RawMiddleNames}".Trim();
}

public static class NameReviewQueue
{
    /// <summary>
    /// Groups reviewable records so a customer carrying the same name on five facilities is ONE
    /// decision. Grouping is by customer + exact raw text (case / whitespace-insensitive), the same
    /// text a stored correction is later matched on.
    /// </summary>
    public static List<NameReviewItem> Group(string fileKind, IEnumerable<(NameReviewSnapshot Snapshot, NameReviewVerdict Verdict)> records)
    {
        var business = string.Equals(fileKind, NameReviewFileKind.Business, StringComparison.OrdinalIgnoreCase);
        var map = new Dictionary<string, NameReviewItem>(StringComparer.Ordinal);

        foreach (var (s, verdict) in records)
        {
            if (!verdict.Reviewable) continue;
            var raw = business ? new[] { s.RawBusinessName } : new[] { s.RawSurname, s.RawFirstName, s.RawMiddleNames };
            var key = s.CustomerKey + "||" + NameReviewText.ExactKey(raw);

            if (!map.TryGetValue(key, out var item))
            {
                item = new NameReviewItem
                {
                    FileKind = fileKind, CustomerKey = s.CustomerKey,
                    RawSurname = s.RawSurname, RawFirstName = s.RawFirstName, RawMiddleNames = s.RawMiddleNames, RawBusinessName = s.RawBusinessName,
                };
                map[key] = item;
            }
            item.RecordCount++;
            foreach (var flag in verdict.Flags) if (!item.Flags.Contains(flag)) item.Flags.Add(flag);
            if (item.AnchorDob.Length == 0) item.AnchorDob = s.Dob;
            item.AnchorIds = string.Join("|", (item.AnchorIds + "|" + s.AnchorIds)
                .Split('|', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal));
        }

        return map.Values.OrderBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase).ThenBy(i => i.CustomerKey, StringComparer.Ordinal).ToList();
    }
}

/// <summary>What the reviewer chose. For Approved, the name fields are the final text (identical to raw = approve as-is).</summary>
public sealed record NameReviewDecision(string Status, string Surname = "", string FirstName = "", string MiddleNames = "", string BusinessName = "", string Note = "");

public static class NameReviewCapture
{
    /// <summary>
    /// Builds the log row for a decision, or an error message for the screen to show. A name flagged
    /// for special characters cannot be approved as-is: the loader rejects those characters, so it must
    /// be corrected, and the corrected text must itself be clean.
    /// </summary>
    /// <param name="hasInvalidCharacters">The cleaner's own character test (individual or business).</param>
    public static (NameReviewLogEntry? Entry, string? Error) Build(
        NameReviewItem item, string subscriberCode, NameReviewDecision decision, string decidedBy, DateTime nowUtc,
        Func<string, bool> hasInvalidCharacters)
    {
        if (decision.Status != NameReviewStatus.Approved && decision.Status != NameReviewStatus.Rejected)
            return (null, "A decision must be Approved or Rejected.");
        if (string.IsNullOrWhiteSpace(subscriberCode)) return (null, "The subscriber could not be determined from the file name.");
        if (string.IsNullOrWhiteSpace(decidedBy)) return (null, "The reviewer could not be identified.");

        var approved = decision.Status == NameReviewStatus.Approved;
        // Rejections never change the name; whatever the screen sent is ignored.
        var surname = approved ? decision.Surname.Trim() : item.RawSurname;
        var first = approved ? decision.FirstName.Trim() : item.RawFirstName;
        var middle = approved ? decision.MiddleNames.Trim() : item.RawMiddleNames;
        var business = approved ? decision.BusinessName.Trim() : item.RawBusinessName;

        if (approved)
        {
            if (item.IsBusiness)
            {
                if (business.Length == 0) return (null, "The business name cannot be empty.");
            }
            else if (surname.Length == 0 || first.Length == 0)
            {
                return (null, "Surname and first name cannot be empty.");
            }

            if (item.Flags.Contains(NameReviewFlagType.SpecialCharacter))
            {
                var parts = item.IsBusiness ? new[] { business } : new[] { surname, first, middle };
                if (parts.Any(hasInvalidCharacters))
                    return (null, "This name has characters the loader rejects. Correct them before approving.");
            }
        }

        if (surname.Length > 200 || first.Length > 200 || middle.Length > 200) return (null, "A name part is longer than 200 characters.");
        if (business.Length > 400) return (null, "The business name is longer than 400 characters.");
        if (decidedBy.Length > 256) return (null, "The reviewer name is too long.");

        return (new NameReviewLogEntry
        {
            FileKind = item.FileKind,
            SubscriberCode = subscriberCode,
            CustomerId = item.CustomerKey,
            FlagTypes = string.Join("|", item.Flags),
            RawSurname = item.RawSurname, RawFirstName = item.RawFirstName, RawMiddleNames = item.RawMiddleNames, RawBusinessName = item.RawBusinessName,
            CorrectedSurname = surname, CorrectedFirstName = first, CorrectedMiddleNames = middle, CorrectedBusinessName = business,
            AnchorDob = item.AnchorDob,
            AnchorIds = item.AnchorIds,
            Status = decision.Status,
            DecidedBy = decidedBy,
            DecidedAtUtc = nowUtc,
            Note = decision.Note.Length > 500 ? decision.Note[..500] : decision.Note,
        }, null);
    }
}
