using CleanserBlazorUI.Entities;
using CleanserBlazorUI.Helpers;

public static class QueueSpec
{
    static CellState Cell(string name, bool passed, params string[] errors) => new(name, passed, errors);
    static string CK(string? s) => (s ?? "").Trim().TrimStart('0').ToUpperInvariant();
    static readonly NameProbe NoProbe = new();
    const string KeywordMsg = "INVALID NAME: BUSINESS KEYWORDS DETECTED IN INDIVIDUAL RECORD — USE COMMERCIAL FILE TYPE";
    const string MandatoryMsg = " SURNAME AND FIRSTNAME ARE MANDATORY AND SHOULD CONTAIN VALID NAMES ";
    const string SpecialMsg = " NAMES CANNOT CONTAIN SEPECIAL CHARACTER(S) ";
    const string AndMsg = "INVALID NAME: MULTIPLE INDIVIDUALS DETECTED — A FACILITY RECORD MUST BE ASSIGNED TO A SINGLE PERSON";

    static NameReviewSnapshot Snap(string cust, string s, string f, string m = "", string biz = "", string dob = "19850101", string ids = "GHA7158610148")
        => new() { CustomerKey = cust, RawSurname = s, RawFirstName = f, RawMiddleNames = m, RawBusinessName = biz, Dob = dob, AnchorIds = ids };

    static NameReviewItem Item(string kind, string cust, string s, string f, string biz, params string[] flags)
    {
        var verdict = new NameReviewVerdict(true, flags, "");
        return NameReviewQueue.Group(kind, new[] { (kind == "BUS" ? Snap(cust, "", "", "", biz) : Snap(cust, s, f), verdict) }).Single();
    }

    public static void Run(Action<string, bool> check)
    {
        // ── classifier: individuals ─────────────────────────────────────
        var v = NameReviewClassifier.Classify("IND", new[] { Cell("Surname", false, KeywordMsg), Cell("FirstName", false), Cell("CustomerID", true) }, NoProbe);
        check("IND keyword-only is reviewable", v.Reviewable && v.Flags.SequenceEqual(new[] { "Keyword" }));
        v = NameReviewClassifier.Classify("IND", new[] { Cell("Surname", false, MandatoryMsg), Cell("FirstName", false) }, new NameProbe(RestrictedWord: true));
        check("IND restricted word (mandatory msg + probe) is reviewable", v.Reviewable && v.Flags.SequenceEqual(new[] { "RestrictedWord" }));
        v = NameReviewClassifier.Classify("IND", new[] { Cell("Surname", false, MandatoryMsg), Cell("FirstName", false) }, NoProbe);
        check("IND blank/invalid surname (same message, no restricted word) is NOT reviewable", !v.Reviewable && v.WhyNot.Contains("missing or invalid"));
        v = NameReviewClassifier.Classify("IND", new[] { Cell("Surname", false, SpecialMsg), Cell("FirstName", false, SpecialMsg) }, NoProbe);
        check("IND special characters reviewable", v.Reviewable && v.Flags.SequenceEqual(new[] { "SpecialCharacter" }));
        v = NameReviewClassifier.Classify("IND", new[] { Cell("Surname", false, KeywordMsg, MandatoryMsg), Cell("FirstName", false) }, new NameProbe(RestrictedWord: true));
        check("IND keyword + restricted: both flags, fixed order", v.Reviewable && v.Flags.SequenceEqual(new[] { "Keyword", "RestrictedWord" }));
        v = NameReviewClassifier.Classify("IND", new[] { Cell("Surname", false, KeywordMsg, AndMsg), Cell("FirstName", false) }, NoProbe);
        check("IND 'AND' message -> NOT reviewable", !v.Reviewable && v.WhyNot.Contains("more than one individual"));
        v = NameReviewClassifier.Classify("IND", new[] { Cell("Surname", false, KeywordMsg), Cell("FirstName", false) }, new NameProbe(MultipleIndividuals: true));
        check("IND 'AND' detected by probe -> NOT reviewable", !v.Reviewable);
        v = NameReviewClassifier.Classify("IND", new[] { Cell("Surname", false, KeywordMsg), Cell("DateOfBirth", false, "DATEOFBIRTH: INVALID DATE") }, NoProbe);
        check("name flag + another error -> NOT reviewable", !v.Reviewable && v.WhyNot.StartsWith("other error"));
        v = NameReviewClassifier.Classify("IND", new[] { Cell("Surname", false, KeywordMsg), Cell("NatIDNum", false) }, NoProbe);
        check("non-name cell failed with no message -> NOT reviewable", !v.Reviewable && v.WhyNot.Contains("NatIDNum"));
        v = NameReviewClassifier.Classify("IND", new[] { Cell("Surname", true), Cell("FirstName", true) }, NoProbe);
        check("no errors -> NOT reviewable", !v.Reviewable);
        v = NameReviewClassifier.Classify("IND", new[] { Cell("Surname", false, " SOMETHING UNEXPECTED ") }, NoProbe);
        check("unknown message on a name cell -> NOT reviewable", !v.Reviewable);

        // ── classifier: businesses ──────────────────────────────────────
        v = NameReviewClassifier.Classify("BUS", new[] { Cell("Businessname", false, " Invalid Business Name ") }, NoProbe);
        check("BUS invalid business name reviewable", v.Reviewable && v.Flags.SequenceEqual(new[] { "InvalidBusinessName" }));
        v = NameReviewClassifier.Classify("BUS", new[] { Cell("Businessname", false, "BUSINESSNAME: INVALID CHARACTERS") }, NoProbe);
        check("BUS invalid characters reviewable", v.Reviewable && v.Flags.SequenceEqual(new[] { "SpecialCharacter" }));
        v = NameReviewClassifier.Classify("BUS", new[] { Cell("Businessname", false, " Invalid Business Name ") }, new NameProbe(SpecialCharacter: true));
        check("BUS hidden special-char problem restored by the probe", v.Reviewable && v.Flags.SequenceEqual(new[] { "InvalidBusinessName", "SpecialCharacter" }));
        v = NameReviewClassifier.Classify("BUS", new[] { Cell("Tradingname", false, "Tradingname: INVALID CHARACTERS") }, NoProbe);
        check("BUS trading-name error -> NOT reviewable (no columns to record it)", !v.Reviewable);
        v = NameReviewClassifier.Classify("BUS", new[] { Cell("Businessname", false, " Invalid Business Name "), Cell("CustomerID", false, "CUSTOMER ID 5 IS LINKED TO DIFFERENT BUSINESSES: A, B — VERIFY WHICH IS CORRECT") }, NoProbe);
        check("BUS name flag + identity conflict -> NOT reviewable", !v.Reviewable);

        // ── grouping ────────────────────────────────────────────────────
        var yes = new NameReviewVerdict(true, new[] { "Keyword" }, "");
        var no = new NameReviewVerdict(false, Array.Empty<string>(), "x");
        var g = NameReviewQueue.Group("IND", new[] { (Snap("615268", "SMART", "ABED"), yes), (Snap("615268", " smart ", "abed"), yes) });
        check("same customer + same raw name (case/space-insensitive) = ONE item, count 2", g.Count == 1 && g[0].RecordCount == 2);
        g = NameReviewQueue.Group("IND", new[] { (Snap("615268", "SMART", "ABED"), yes), (Snap("777001", "SMART", "ABED"), yes) });
        check("different customers = separate decisions", g.Count == 2);
        g = NameReviewQueue.Group("IND", new[] { (Snap("1", "SMART", "ABED"), yes), (Snap("2", "OTHER", "NAME"), no) });
        check("non-reviewable records are ignored", g.Count == 1);
        g = NameReviewQueue.Group("IND", new[] { (Snap("1", "SMART", "ABED", dob: "", ids: "AB12345"), new NameReviewVerdict(true, new[] { "Keyword" }, "")),
                                                  (Snap("1", "SMART", "ABED", dob: "19850101", ids: "GHA7158610148|AB12345"), new NameReviewVerdict(true, new[] { "RestrictedWord" }, "")) });
        check("anchors/flags are unioned; first non-empty DOB kept", g.Count == 1 && g[0].AnchorDob == "19850101"
              && g[0].AnchorIds == "AB12345|GHA7158610148" && g[0].Flags.SequenceEqual(new[] { "Keyword", "RestrictedWord" }));
        g = NameReviewQueue.Group("IND", new[] { (Snap("1", "ZED", "A"), yes), (Snap("1", "ABLE", "B"), yes) });
        check("ordered by display name", g[0].RawSurname == "ABLE");

        // ── capture / validation ────────────────────────────────────────
        Func<string, bool> hasBad = s => s.Contains('#') || s.Contains('$');
        var now = new DateTime(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);
        var kw = Item("IND", "615268", "SMART", "ABED", "", "Keyword");
        var (e, err) = NameReviewCapture.Build(kw, "ADB", new NameReviewDecision("Approved", "SMART", "ABED"), "a@b.com", now, hasBad);
        check("approve as-is: corrected == raw, anchors + flags carried", e != null && e.CorrectedSurname == "SMART" && e.RawSurname == "SMART"
              && e.FlagTypes == "Keyword" && e.AnchorDob == "19850101" && e.AnchorIds == "GHA7158610148" && e.DecidedBy == "a@b.com" && e.Status == "Approved");
        var sp = Item("IND", "615268", "SMITH#", "JOHN", "", "SpecialCharacter");
        (e, err) = NameReviewCapture.Build(sp, "ADB", new NameReviewDecision("Approved", "SMITH#", "JOHN"), "a@b.com", now, hasBad);
        check("special-char name cannot be approved as-is", e == null && err!.Contains("loader rejects"));
        (e, err) = NameReviewCapture.Build(sp, "ADB", new NameReviewDecision("Approved", "SMITH", "JOHN"), "a@b.com", now, hasBad);
        check("special-char name approved once corrected clean", e != null && e.RawSurname == "SMITH#" && e.CorrectedSurname == "SMITH");
        (e, err) = NameReviewCapture.Build(kw, "ADB", new NameReviewDecision("Approved", "", "ABED"), "a@b.com", now, hasBad);
        check("approve with empty surname rejected", e == null);
        (e, err) = NameReviewCapture.Build(kw, "ADB", new NameReviewDecision("Rejected", "CHANGED", "NAME"), "a@b.com", now, hasBad);
        check("reject ignores any edits (corrected stays raw)", e != null && e.Status == "Rejected" && e.CorrectedSurname == "SMART" && e.CorrectedFirstName == "ABED");
        (e, err) = NameReviewCapture.Build(kw, "ADB", new NameReviewDecision("Pending"), "a@b.com", now, hasBad);
        check("invalid status rejected", e == null);
        (e, err) = NameReviewCapture.Build(kw, "", new NameReviewDecision("Approved", "SMART", "ABED"), "a@b.com", now, hasBad);
        check("missing subscriber rejected", e == null);
        (e, err) = NameReviewCapture.Build(kw, "ADB", new NameReviewDecision("Approved", "SMART", "ABED"), "", now, hasBad);
        check("missing reviewer rejected", e == null);
        (e, err) = NameReviewCapture.Build(kw, "ADB", new NameReviewDecision("Approved", new string('X', 201), "ABED"), "a@b.com", now, hasBad);
        check("over-long name part rejected", e == null && err!.Contains("200"));
        var bz = Item("BUS", "21715", "", "", "SUMBO WOMEN EMPOWERMENT ORG", "InvalidBusinessName");
        (e, err) = NameReviewCapture.Build(bz, "ADB", new NameReviewDecision("Approved", BusinessName: ""), "a@b.com", now, hasBad);
        check("approve business with empty name rejected", e == null);
        (e, err) = NameReviewCapture.Build(bz, "ADB", new NameReviewDecision("Approved", BusinessName: "SUMBO WOMEN EMPOWERMENT ORG", Note: new string('n', 600)), "a@b.com", now, hasBad);
        check("business approve as-is; note truncated to 500", e != null && e.RawBusinessName == "SUMBO WOMEN EMPOWERMENT ORG" && e.Note.Length == 500 && e.FileKind == "BUS");

        // ── round trip: what the screen saves is what the next run finds ─
        NameReviewIndex Idx(string kind, NameReviewLogEntry x) => new(kind, new[] { x }, CK);
        (e, _) = NameReviewCapture.Build(kw, "ADB", new NameReviewDecision("Approved", "SMART", "ABED"), "a@b.com", now, hasBad);
        check("ROUND TRIP approve keyword -> next run Trusted",
            Idx("IND", e!).Evaluate("615268", NameReviewText.NameKey("SMART", "ABED", ""), "19850101", "GHA7158610148").Decision == NameTrustDecision.Trusted);
        (e, _) = NameReviewCapture.Build(kw, "ADB", new NameReviewDecision("Rejected"), "a@b.com", now, hasBad);
        check("ROUND TRIP reject -> next run Vetoed",
            Idx("IND", e!).Evaluate("615268", NameReviewText.NameKey("SMART", "ABED", ""), "19850101", "GHA7158610148").Decision == NameTrustDecision.Vetoed);
        (e, _) = NameReviewCapture.Build(sp, "ADB", new NameReviewDecision("Approved", "SMITH", "JOHN"), "a@b.com", now, hasBad);
        var ix = Idx("IND", e!);
        check("ROUND TRIP special-char correction found by the raw text", ix.FindCorrection("615268", "SMITH#", "JOHN", "")?.CorrectedSurname == "SMITH");
        check("ROUND TRIP special-char-only entry is not a trust signal", ix.Evaluate("615268", NameReviewText.NameKey("SMITH", "JOHN", ""), "19850101", "GHA7158610148").Decision == NameTrustDecision.NoSignal);
        var both = new NameReviewVerdict(true, new[] { "Keyword", "SpecialCharacter" }, "");
        var bothItem = NameReviewQueue.Group("IND", new[] { (Snap("555", "ABED SMART#", "X"), both) }).Single();
        (e, _) = NameReviewCapture.Build(bothItem, "ADB", new NameReviewDecision("Approved", "ABED SMART", "X"), "a@b.com", now, hasBad);
        var ix2 = Idx("IND", e!);
        check("ROUND TRIP keyword+special: corrected, then trusted on the corrected name",
            ix2.FindCorrection("555", "ABED SMART#", "X", "") != null
            && ix2.Evaluate("555", NameReviewText.NameKey("ABED SMART", "X", ""), "19850101", "GHA7158610148").Decision == NameTrustDecision.Trusted);
        var curly = NameReviewQueue.Group("IND", new[] { (Snap("9", "N\u2019DITU#", "KOFI"), new NameReviewVerdict(true, new[] { "SpecialCharacter" }, "")) }).Single();
        (e, _) = NameReviewCapture.Build(curly, "ADB", new NameReviewDecision("Approved", "N'DITU", "KOFI"), "a@b.com", now, hasBad);
        check("ROUND TRIP raw text with a curly apostrophe matches exactly next run", Idx("IND", e!).FindCorrection("9", "N\u2019DITU#", "KOFI", "") != null);
        (e, _) = NameReviewCapture.Build(bz, "ADB", new NameReviewDecision("Approved", BusinessName: "SUMBO WOMEN EMPOWERMENT ORG"), "a@b.com", now, hasBad);
        check("ROUND TRIP business approve -> next run Trusted",
            Idx("BUS", e!).Evaluate("21715", NameReviewText.NameKey("SUMBO WOMEN EMPOWERMENT ORG"), "", "").Decision == NameTrustDecision.Trusted);
    }
}
