// Scenario suite for the name-review trust / correction logic (Helpers/NameReviewTrust.cs).
// Run from the repo root:  dotnet run --project tools/NameReviewSpec
// Compiles the REAL source files (linked, not copied); not part of CleanserProject.sln.

using CleanserBlazorUI.Entities;
using CleanserBlazorUI.Helpers;

int pass = 0, fail = 0;
void Check(string name, bool ok) { if (ok) pass++; else { fail++; Console.WriteLine("FAIL: " + name); } }

static string CustKey(string? s) => (s ?? "").Trim().TrimStart('0').ToUpperInvariant();
DateTime T(int day) => new DateTime(2026, 10, day, 12, 0, 0, DateTimeKind.Utc);

NameReviewLogEntry Ind(int id, string status, string cust, string rawS, string rawF, string corrS, string corrF,
    string dob, string ids, string flags = "Keyword", int day = 1, int? revokedDay = null) => new()
{
    Id = id, FileKind = "IND", SubscriberCode = "ADB", CustomerId = cust, FlagTypes = flags,
    RawSurname = rawS, RawFirstName = rawF, CorrectedSurname = corrS, CorrectedFirstName = corrF,
    AnchorDob = dob, AnchorIds = ids, Status = status, DecidedBy = "t", DecidedAtUtc = T(day),
    RevokedAtUtc = revokedDay.HasValue ? T(revokedDay.Value) : null,
};
NameReviewLogEntry Bus(int id, string status, string cust, string rawN, string corrN, string ids,
    string flags = "InvalidBusinessName", int day = 1, int? revokedDay = null) => new()
{
    Id = id, FileKind = "BUS", SubscriberCode = "ADB", CustomerId = cust, FlagTypes = flags,
    RawBusinessName = rawN, CorrectedBusinessName = corrN, AnchorIds = ids, Status = status,
    DecidedBy = "t", DecidedAtUtc = T(day), RevokedAtUtc = revokedDay.HasValue ? T(revokedDay.Value) : null,
};
NameReviewIndex IndIdx(params NameReviewLogEntry[] e) => new("IND", e, CustKey);
NameReviewIndex BusIdx(params NameReviewLogEntry[] e) => new("BUS", e, CustKey);
string K(string s, string f = "") => NameReviewText.NameKey(s, f);
NameTrustDecision Ev(NameReviewIndex i, string cust, string s, string f, string dob, string ids)
    => i.Evaluate(cust, K(s, f), dob, ids).Decision;

// ── text helpers ─────────────────────────────────────────────────────────
Check("NameKey order-insensitive", K("ASIEDU ASARE") == K("ASARE ASIEDU"));
Check("NameKey apostrophe variants equal", K("N'DITU") == K("N\u2019DITU") && K("N'DITU") == K("N\u2018DITU") && K("N'DITU") == K("N`DITU"));
Check("NameKey ignores special characters", K("SMITH#") == K("SMITH"));
Check("ExactKey case/whitespace-insensitive", NameReviewText.ExactKey(" smith#  jr ") == NameReviewText.ExactKey("SMITH# JR"));
Check("ExactKey keeps special chars distinct", NameReviewText.ExactKey("SMITH#") != NameReviewText.ExactKey("SMITH$"));
Check("AnchorIds drops placeholders, dedups, normalizes",
    NameReviewText.BuildAnchorIds(new string?[] { "GHA-715861014-8", "STAFF", "0000", "11111", null, "gha715861014-8", "GHANACARD" }) == "GHA7158610148");
Check("AnchorIds sorted and joined",
    NameReviewText.BuildAnchorIds(new string?[] { "ZZ-99999", "AB-12345" }) == "AB12345|ZZ99999");
Check("Dob yyyyMMdd", NameReviewText.NormalizeDob("19850101") == "19850101");
Check("Dob dashed", NameReviewText.NormalizeDob("1985-01-01") == "19850101");
Check("Dob junk -> empty", NameReviewText.NormalizeDob("not a date") == "" && NameReviewText.NormalizeDob(null) == "");

// ── same CustomerID ──────────────────────────────────────────────────────
var abed = Ind(1, "Approved", "615268", "SMART", "ABED", "SMART", "ABED", "19850101", "GHA7158610148");
Check("same cust approved + DOB/ID match -> Trusted", Ev(IndIdx(abed), "615268", "SMART", "ABED", "19850101", "GHA7158610148") == NameTrustDecision.Trusted);
Check("customer id leading zeros normalized", Ev(IndIdx(abed), "0615268", "SMART", "ABED", "19850101", "GHA7158610148") == NameTrustDecision.Trusted);
Check("same cust, DOB differs (both present) -> NoSignal", Ev(IndIdx(abed), "615268", "SMART", "ABED", "19990909", "GHA7158610148") == NameTrustDecision.NoSignal);
Check("same cust, record has no IDs -> still Trusted", Ev(IndIdx(abed), "615268", "SMART", "ABED", "19850101", "") == NameTrustDecision.Trusted);
Check("same cust, record has no DOB -> still Trusted", Ev(IndIdx(abed), "615268", "SMART", "ABED", "", "GHA7158610148") == NameTrustDecision.Trusted);
Check("same cust, IDs both present but disjoint -> NoSignal", Ev(IndIdx(abed), "615268", "SMART", "ABED", "19850101", "AB12345") == NameTrustDecision.NoSignal);
Check("different name -> NoSignal", Ev(IndIdx(abed), "615268", "OTHER", "PERSON", "19850101", "GHA7158610148") == NameTrustDecision.NoSignal);
Check("name order swapped still trusted", Ev(IndIdx(abed), "615268", "ABED", "SMART", "19850101", "GHA7158610148") == NameTrustDecision.Trusted);

// ── different CustomerID, same person ────────────────────────────────────
Check("other cust: name+DOB+ID overlap -> Trusted", Ev(IndIdx(abed), "777001", "SMART", "ABED", "19850101", "GHA7158610148|VV55555") == NameTrustDecision.Trusted);
Check("other cust: no ID overlap -> NoSignal", Ev(IndIdx(abed), "777001", "SMART", "ABED", "19850101", "AB12345") == NameTrustDecision.NoSignal);
Check("other cust: no IDs on record -> NoSignal", Ev(IndIdx(abed), "777001", "SMART", "ABED", "19850101", "") == NameTrustDecision.NoSignal);
Check("other cust: DOB missing on record -> NoSignal", Ev(IndIdx(abed), "777001", "SMART", "ABED", "", "GHA7158610148") == NameTrustDecision.NoSignal);
Check("other cust: DOB differs -> NoSignal", Ev(IndIdx(abed), "777001", "SMART", "ABED", "19990909", "GHA7158610148") == NameTrustDecision.NoSignal);
Check("blank customer id falls to strong rule only", Ev(IndIdx(abed), "", "SMART", "ABED", "19850101", "GHA7158610148") == NameTrustDecision.Trusted
    && Ev(IndIdx(abed), "", "SMART", "ABED", "19850101", "") == NameTrustDecision.NoSignal);

// ── veto ─────────────────────────────────────────────────────────────────
var rej = Ind(2, "Rejected", "615268", "SMART", "ABED", "SMART", "ABED", "19850101", "GHA7158610148");
Check("same cust rejected -> Vetoed", Ev(IndIdx(rej), "615268", "SMART", "ABED", "19850101", "GHA7158610148") == NameTrustDecision.Vetoed);
Check("same cust rejected vetoes even if DOB/ID differ", Ev(IndIdx(rej), "615268", "SMART", "ABED", "19990909", "AB12345") == NameTrustDecision.Vetoed);
Check("other cust rejected, uncorroborated -> NoSignal", Ev(IndIdx(rej), "777001", "SMART", "ABED", "19850101", "AB12345") == NameTrustDecision.NoSignal);
Check("other cust rejected, corroborated -> Vetoed", Ev(IndIdx(rej), "777001", "SMART", "ABED", "19850101", "GHA7158610148") == NameTrustDecision.Vetoed);

// ── latest decision wins ─────────────────────────────────────────────────
var a1 = Ind(1, "Approved", "615268", "SMART", "ABED", "SMART", "ABED", "19850101", "GHA7158610148", day: 1);
var r2 = Ind(2, "Rejected", "615268", "SMART", "ABED", "SMART", "ABED", "19850101", "GHA7158610148", day: 2);
var a3 = Ind(3, "Approved", "615268", "SMART", "ABED", "SMART", "ABED", "19850101", "GHA7158610148", day: 3);
Check("approve then reject -> Vetoed", Ev(IndIdx(a1, r2), "615268", "SMART", "ABED", "19850101", "GHA7158610148") == NameTrustDecision.Vetoed);
Check("order of rows irrelevant", Ev(IndIdx(r2, a1), "615268", "SMART", "ABED", "19850101", "GHA7158610148") == NameTrustDecision.Vetoed);
Check("approve, reject, approve again -> Trusted", Ev(IndIdx(a1, r2, a3), "615268", "SMART", "ABED", "19850101", "GHA7158610148") == NameTrustDecision.Trusted);
var revokedInPlace = Ind(1, "Revoked", "615268", "SMART", "ABED", "SMART", "ABED", "19850101", "GHA7158610148", day: 1, revokedDay: 5);
Check("revoked in place -> Vetoed", Ev(IndIdx(revokedInPlace), "615268", "SMART", "ABED", "19850101", "GHA7158610148") == NameTrustDecision.Vetoed);
var reApproved = Ind(2, "Approved", "615268", "SMART", "ABED", "SMART", "ABED", "19850101", "GHA7158610148", day: 8);
Check("revoked in place, then re-approved later -> Trusted", Ev(IndIdx(revokedInPlace, reApproved), "615268", "SMART", "ABED", "19850101", "GHA7158610148") == NameTrustDecision.Trusted);
var reApprovedBefore = Ind(2, "Approved", "615268", "SMART", "ABED", "SMART", "ABED", "19850101", "GHA7158610148", day: 3);
Check("revoked in place after a later-dated approval -> Vetoed", Ev(IndIdx(revokedInPlace, reApprovedBefore), "615268", "SMART", "ABED", "19850101", "GHA7158610148") == NameTrustDecision.Vetoed);

// ── special-character-only entries are never trust/veto ──────────────────
var spApproved = Ind(4, "Approved", "615268", "SMITH#", "JOHN", "SMITH", "JOHN", "19850101", "GHA7158610148", flags: "SpecialCharacter");
var spRejected = Ind(5, "Rejected", "615268", "SMITH#", "JOHN", "SMITH#", "JOHN", "19850101", "GHA7158610148", flags: "SpecialCharacter");
Check("special-char-only approved: no trust", Ev(IndIdx(spApproved), "615268", "SMITH", "JOHN", "19850101", "GHA7158610148") == NameTrustDecision.NoSignal);
Check("special-char-only rejected: no veto on the clean spelling", Ev(IndIdx(spRejected), "615268", "SMITH", "JOHN", "19850101", "GHA7158610148") == NameTrustDecision.NoSignal);
var both = Ind(6, "Approved", "615268", "ABED SMART#", "", "ABED SMART", "", "19850101", "GHA7158610148", flags: "Keyword|SpecialCharacter");
Check("Keyword|SpecialCharacter entry counts (corrected name trusted)", Ev(IndIdx(both), "615268", "ABED SMART", "", "19850101", "GHA7158610148") == NameTrustDecision.Trusted);

// ── file kind / subscriber isolation ─────────────────────────────────────
Check("BUS entry ignored by IND index", Ev(IndIdx(Bus(9, "Approved", "615268", "SMART", "SMART", "AB12345")), "615268", "SMART", "", "", "AB12345") == NameTrustDecision.NoSignal);

// ── corrections ──────────────────────────────────────────────────────────
var fix = Ind(10, "Approved", "615268", "SMITH#", "JOHN", "SMITH", "JOHN", "19850101", "GHA7158610148", flags: "SpecialCharacter");
var ix = IndIdx(fix);
Check("correction found for same customer + exact raw", ix.FindCorrection("615268", "SMITH#", "JOHN", "")?.Id == 10);
Check("correction matches case/whitespace-insensitively", ix.FindCorrection("615268", " smith# ", "john", "")?.Id == 10);
Check("correction customer normalized", ix.FindCorrection("0615268", "SMITH#", "JOHN", "")?.Id == 10);
Check("no correction for another customer", ix.FindCorrection("777001", "SMITH#", "JOHN", "") == null);
Check("no correction for a different special char", ix.FindCorrection("615268", "SMITH$", "JOHN", "") == null);
Check("no correction for already-clean spelling", ix.FindCorrection("615268", "SMITH", "JOHN", "") == null);
Check("approved as-is (no change) -> no correction", IndIdx(Ind(11, "Approved", "615268", "SMART", "ABED", "SMART", "ABED", "", "")).FindCorrection("615268", "SMART", "ABED", "") == null);
Check("rejected -> no correction", IndIdx(Ind(12, "Rejected", "615268", "SMITH#", "JOHN", "SMITH", "JOHN", "", "")).FindCorrection("615268", "SMITH#", "JOHN", "") == null);
var fixRevoked = Ind(10, "Revoked", "615268", "SMITH#", "JOHN", "SMITH", "JOHN", "", "", day: 1, revokedDay: 4);
Check("revoked correction no longer applies", IndIdx(fixRevoked).FindCorrection("615268", "SMITH#", "JOHN", "") == null);
Check("blank customer id -> no correction", ix.FindCorrection("", "SMITH#", "JOHN", "") == null);

// ── business ─────────────────────────────────────────────────────────────
var sumbo = Bus(20, "Approved", "21715", "SUMBO WOMEN EMPOWERMENT ORG", "SUMBO WOMEN EMPOWERMENT ORG", "CG082162019|C0014604884");
var bx = BusIdx(sumbo);
NameTrustDecision BEv(string cust, string name, string ids) => bx.Evaluate(cust, NameReviewText.NameKey(name), "", ids).Decision;
Check("BUS same cust: ID overlap -> Trusted", BEv("21715", "SUMBO WOMEN EMPOWERMENT ORG", "CG082162019") == NameTrustDecision.Trusted);
Check("BUS same cust: no IDs -> Trusted", BEv("21715", "SUMBO WOMEN EMPOWERMENT ORG", "") == NameTrustDecision.Trusted);
Check("BUS same cust: disjoint IDs -> NoSignal", BEv("21715", "SUMBO WOMEN EMPOWERMENT ORG", "CS162712018") == NameTrustDecision.NoSignal);
Check("BUS other cust: ID overlap -> Trusted (no DOB needed)", BEv("99999", "SUMBO WOMEN EMPOWERMENT ORG", "C0014604884") == NameTrustDecision.Trusted);
Check("BUS other cust: no overlap -> NoSignal", BEv("99999", "SUMBO WOMEN EMPOWERMENT ORG", "CS162712018") == NameTrustDecision.NoSignal);
Check("BUS other cust: no IDs -> NoSignal", BEv("99999", "SUMBO WOMEN EMPOWERMENT ORG", "") == NameTrustDecision.NoSignal);
Check("BUS rejected same cust -> Vetoed", BusIdx(Bus(21, "Rejected", "21715", "SUMBO WOMEN EMPOWERMENT ORG", "SUMBO WOMEN EMPOWERMENT ORG", ""))
    .Evaluate("21715", NameReviewText.NameKey("SUMBO WOMEN EMPOWERMENT ORG"), "", "").Decision == NameTrustDecision.Vetoed);
var busFix = Bus(22, "Approved", "21715", "ACME & SONS#", "ACME & SONS", "AB12345", flags: "SpecialCharacter");
Check("BUS correction found", BusIdx(busFix).FindCorrection("21715", "ACME & SONS#")?.CorrectedBusinessName == "ACME & SONS");

Console.WriteLine($"{pass} passed, {fail} failed");
return fail == 0 ? 0 : 1;
