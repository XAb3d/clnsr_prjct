using System.Globalization;
using System.Text;
using CleanserBlazorUI.Entities;

namespace CleanserBlazorUI.Helpers;

public enum NameTrustDecision
{
    /// <summary>No applicable review decision -- existing behaviour (reference trust, fuzzy match) is untouched.</summary>
    NoSignal = 0,
    /// <summary>A reviewer approved this name for this person.</summary>
    Trusted = 1,
    /// <summary>A reviewer rejected / revoked this name; overrides every other trust path.</summary>
    Vetoed = 2,
}

public sealed record NameTrustResult(NameTrustDecision Decision, string Reason);

/// <summary>
/// Pure text helpers for the name-review feature. Kept free of any app state so
/// that capturing a decision (Name Review screen) and applying it (cleaning run)
/// produce identical keys -- both MUST use these, at the same point in the
/// pipeline (right after the file is read, before ISCleaned), or they won't line up.
/// </summary>
public static class NameReviewText
{
    private static readonly char[] NameSplitChars = { ' ', '-', '\'', '.', ',' };

    /// <summary>
    /// Letters-only, order-insensitive key: upper-case, split on space - ' . ,
    /// keep letters only, sort the tokens, concatenate. Same idea as the
    /// cleaner's NormaliseNameSortedTokens, but for any number of name parts.
    /// "ASIEDU ASARE" and "ASARE ASIEDU" give the same key.
    /// </summary>
    public static string NameKey(params string?[] parts)
    {
        // Apostrophe style varies file to file (Word/Excel autocorrect), and the cleaner only
        // normalizes it later, inside ISCleaned -- after this key is computed. Unify it here so
        // N'DITU, N’DITU and N‘DITU all key the same (a straight ' splits tokens; a curly one wouldn't).
        var raw = UnifyApostrophes(string.Join(" ", parts.Select(p => p ?? string.Empty))).ToUpperInvariant();
        var tokens = raw.Split(NameSplitChars, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => new string(t.Where(char.IsLetter).ToArray()))
            .Where(t => t.Length > 0)
            .OrderBy(t => t, StringComparer.Ordinal);
        return string.Concat(tokens);
    }

    private static string UnifyApostrophes(string s)
        => s.Replace('\u2018', '\'').Replace('\u2019', '\'').Replace('\u02BC', '\'')
            .Replace('\u02BB', '\'').Replace('\u00B4', '\'').Replace('\u2032', '\'').Replace('`', '\'');

    /// <summary>
    /// Exact-text key (trim, collapse whitespace, upper-case; parts joined with '|').
    /// Used to match a correction to the exact raw text a reviewer fixed -- NOT
    /// letter-only, because the characters being corrected are exactly what
    /// NameKey strips.
    /// </summary>
    public static string ExactKey(params string?[] parts)
        => string.Join("|", parts.Select(p => CollapseWhitespace((p ?? string.Empty).Trim().ToUpperInvariant())));

    private static string CollapseWhitespace(string s)
    {
        var sb = new StringBuilder(s.Length);
        var previousWasSpace = false;
        foreach (var c in s)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!previousWasSpace) sb.Append(' ');
                previousWasSpace = true;
            }
            else
            {
                sb.Append(c);
                previousWasSpace = false;
            }
        }
        return sb.ToString();
    }

    public static string NormalizeId(string? value)
        => value == null ? string.Empty : new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    /// <summary>Placeholders / labels / junk are not identity evidence: too short, no digit, or one repeated character.</summary>
    public static bool IsUsableId(string normalized)
    {
        if (normalized.Length < 5) return false;
        if (!normalized.Any(char.IsDigit)) return false;
        if (normalized.All(c => c == normalized[0])) return false;
        return true;
    }

    /// <summary>'|'-delimited, sorted, distinct, normalized, usable ID values -- the format stored in NameReviewLogEntry.AnchorIds.</summary>
    public static string BuildAnchorIds(IEnumerable<string?> rawIds)
        => string.Join("|", rawIds.Select(NormalizeId)
            .Where(IsUsableId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(v => v, StringComparer.Ordinal));

    /// <summary>yyyyMMdd, or empty when the value isn't a recognisable date (empty means "unknown", never a match).</summary>
    public static string NormalizeDob(string? dob)
    {
        if (string.IsNullOrWhiteSpace(dob)) return string.Empty;
        var digits = new string(dob.Where(char.IsDigit).ToArray());
        if (digits.Length == 8 && (digits.StartsWith("19") || digits.StartsWith("20"))) return digits;
        if (DateTime.TryParse(dob, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        return string.Empty;
    }
}

/// <summary>
/// Reviewer decisions for ONE subscriber and ONE file kind, indexed for the
/// per-record lookups a cleaning run does. Build once per run, then call
/// <see cref="FindCorrection"/> and <see cref="Evaluate"/> per record.
///
/// Trust rules (see NameReviewLogEntry):
///  - Same CustomerID: an Approved entry counts when the name matches and DOB /
///    IDs don't contradict (DOB equal when both present; IDs overlap, or one side has none).
///  - Different CustomerID (same person): requires name match, DOB equal (IND),
///    and at least one valid ID in common.
///  - Rejected / Revoked entries are a veto. Same CustomerID always applies;
///    a different CustomerID applies only when corroborated the same strong way.
///  - Among applicable entries the LATEST decision wins.
///  - Entries flagged ONLY as SpecialCharacter are never trust/veto signals: a
///    special character is corrected, not trusted, and is never bypassed.
/// </summary>
public sealed class NameReviewIndex
{
    private sealed class Item
    {
        public NameReviewLogEntry Row = null!;
        public string CustomerKey = string.Empty;
        public string RawKey = string.Empty;
        public string CorrectedKey = string.Empty;
        public string RawExact = string.Empty;
        public string CorrectedExact = string.Empty;
        public DateTime EffectiveAt;
        public HashSet<string> Ids = new(StringComparer.Ordinal);
    }

    private static readonly NameTrustResult NoSignalResult = new(NameTrustDecision.NoSignal, string.Empty);

    private readonly string _fileKind;
    private readonly Func<string?, string> _customerKey;
    private readonly Dictionary<string, List<Item>> _byCustomer = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Item>> _byNameKey = new(StringComparer.Ordinal);

    private bool IsBusiness => string.Equals(_fileKind, NameReviewFileKind.Business, StringComparison.OrdinalIgnoreCase);

    /// <param name="customerKey">The cleaner's CustomerID normalizer, so entry and record customer IDs compare the same way.</param>
    public NameReviewIndex(string fileKind, IEnumerable<NameReviewLogEntry> entries, Func<string?, string> customerKey)
    {
        _fileKind = fileKind;
        _customerKey = customerKey;
        foreach (var row in entries)
        {
            if (!string.Equals(row.FileKind, fileKind, StringComparison.OrdinalIgnoreCase)) continue;
            if (row.Status != NameReviewStatus.Approved && row.Status != NameReviewStatus.Rejected && row.Status != NameReviewStatus.Revoked) continue;

            var item = BuildItem(row);
            AddTo(_byCustomer, item.CustomerKey, item);
            if (!IsSpecialCharacterOnly(row.FlagTypes))
            {
                AddTo(_byNameKey, item.RawKey, item);
                if (item.CorrectedKey != item.RawKey) AddTo(_byNameKey, item.CorrectedKey, item);
            }
        }
    }

    private Item BuildItem(NameReviewLogEntry row)
    {
        string?[] raw = IsBusiness
            ? new[] { row.RawBusinessName }
            : new[] { row.RawSurname, row.RawFirstName, row.RawMiddleNames };
        string?[] corrected = IsBusiness
            ? new[] { row.CorrectedBusinessName }
            : new[] { row.CorrectedSurname, row.CorrectedFirstName, row.CorrectedMiddleNames };
        // Rows with no correction columns filled (e.g. approved as-is) fall back to the raw text.
        if (corrected.All(string.IsNullOrWhiteSpace)) corrected = raw;

        return new Item
        {
            Row = row,
            CustomerKey = _customerKey(row.CustomerId),
            RawKey = NameReviewText.NameKey(raw),
            CorrectedKey = NameReviewText.NameKey(corrected),
            RawExact = NameReviewText.ExactKey(raw),
            CorrectedExact = NameReviewText.ExactKey(corrected),
            // A revoked approval can be recorded either by updating the row or by inserting a new one;
            // using the revocation time for revoked rows makes "latest decision" right in both cases.
            EffectiveAt = row.Status == NameReviewStatus.Revoked && row.RevokedAtUtc.HasValue ? row.RevokedAtUtc.Value : row.DecidedAtUtc,
            Ids = SplitIds(row.AnchorIds),
        };
    }

    private static void AddTo(Dictionary<string, List<Item>> map, string key, Item item)
    {
        if (!map.TryGetValue(key, out var list)) map[key] = list = new List<Item>();
        list.Add(item);
    }

    private static HashSet<string> SplitIds(string? joined)
        => new((joined ?? string.Empty).Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), StringComparer.Ordinal);

    private static bool IsSpecialCharacterOnly(string? flagTypes)
    {
        var flags = (flagTypes ?? string.Empty).Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return flags.Length > 0 && flags.All(f => string.Equals(f, NameReviewFlagType.SpecialCharacter, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLater(Item candidate, Item? current)
        => current == null
           || candidate.EffectiveAt > current.EffectiveAt
           || (candidate.EffectiveAt == current.EffectiveAt && candidate.Row.Id > current.Row.Id);

    /// <summary>
    /// The approved correction to apply to this record's raw name, or null.
    /// Matches the SAME CustomerID and the exact raw text (case/whitespace-insensitive);
    /// the latest decision for that text must be an approval whose corrected text differs from the raw.
    /// </summary>
    public NameReviewLogEntry? FindCorrection(string? customerId, params string?[] rawParts)
    {
        var custKey = _customerKey(customerId);
        if (custKey.Length == 0 || !_byCustomer.TryGetValue(custKey, out var items)) return null;

        var exact = NameReviewText.ExactKey(rawParts);
        Item? best = null;
        foreach (var item in items)
        {
            if (item.RawExact != exact) continue;
            if (IsLater(item, best)) best = item;
        }
        if (best == null || best.Row.Status != NameReviewStatus.Approved) return null;
        return best.CorrectedExact == best.RawExact ? null : best.Row;
    }

    /// <param name="nameKey">NameReviewText.NameKey of the record's name AFTER any correction.</param>
    /// <param name="dob">NameReviewText.NormalizeDob of the record's DOB ("" for businesses).</param>
    /// <param name="anchorIds">NameReviewText.BuildAnchorIds of the record's ID values.</param>
    public NameTrustResult Evaluate(string? customerId, string nameKey, string dob, string anchorIds)
    {
        if (string.IsNullOrEmpty(nameKey) || !_byNameKey.TryGetValue(nameKey, out var candidates)) return NoSignalResult;

        var custKey = _customerKey(customerId);
        var recordIds = SplitIds(anchorIds);
        Item? best = null;
        foreach (var item in candidates)
        {
            var sameCustomer = custKey.Length > 0 && item.CustomerKey == custKey;
            var applicable = sameCustomer
                ? item.Row.Status != NameReviewStatus.Approved || Corroborates(item, dob, recordIds, strong: false)
                : Corroborates(item, dob, recordIds, strong: true);
            if (applicable && IsLater(item, best)) best = item;
        }
        if (best == null) return NoSignalResult;

        var when = best.EffectiveAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return best.Row.Status == NameReviewStatus.Approved
            ? new NameTrustResult(NameTrustDecision.Trusted, $"APPROVED {when}")
            : new NameTrustResult(NameTrustDecision.Vetoed, $"{best.Row.Status.ToUpperInvariant()} {when}");
    }

    private bool Corroborates(Item item, string dob, HashSet<string> recordIds, bool strong)
    {
        if (!IsBusiness)
        {
            var entryDob = item.Row.AnchorDob ?? string.Empty;
            var both = entryDob.Length > 0 && dob.Length > 0;
            if (strong ? !(both && entryDob == dob) : (both && entryDob != dob)) return false;
        }
        var overlap = item.Ids.Overlaps(recordIds);
        if (strong) return overlap;
        return item.Ids.Count == 0 || recordIds.Count == 0 || overlap;
    }
}
