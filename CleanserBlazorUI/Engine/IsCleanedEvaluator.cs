using CleanserBlazorUI.Entities;
using CleanserBlazorUI.Helpers;

namespace CleanserBlazorUI.Engine;

/// <summary>
/// Row-level CLEAN vs UNL decision copied from Home.razor ISCleaned for IND and BUS.
/// Cross-record flags are left empty when called from the CKMS API.
/// </summary>
public class IsCleanedEvaluator
{
    readonly StringHelper stringHelper = new();
    readonly List<string> settingDataSettings;
    readonly Dictionary<string, string> shortForms;

    public IsCleanedEvaluator(
        List<string>? businessKeywords = null,
        Dictionary<string, string>? shortFormMap = null)
    {
        settingDataSettings = businessKeywords ?? new List<string>();
        shortForms = shortFormMap ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    string filename = string.Empty;
    string isSubscriber_X_or_Y = string.Empty;

    public bool Evaluate(BusinessContext bc)
    {
        if (bc == null) return false;

                    var _date = stringHelper.CheckDate(bc.DisbursementDate.Data);

                    if (stringHelper.StartsWithFourXOrZero(bc.Busregnum.Data))
                    {
                        bc.Busregnum.Data = string.Empty;
                    }
                    if (stringHelper.StartsWithFourXOrZero(bc.Tinum.Data))
                    {
                        bc.Tinum.Data = string.Empty;
                    }
                    var Busregnum_Tinum = stringHelper.Busregnum_Tinum(bc.Busregnum.Data, bc.Tinum.Data);
                    bc.Busregnum = Busregnum_Tinum[0];
                    bc.Tinum = Busregnum_Tinum[1];

                    if (string.IsNullOrWhiteSpace(bc.Busregnum.Data) &&
                        string.IsNullOrWhiteSpace(bc.Tinum.Data) &&
                        _date.IsValidFormat && _date.TheDate.Year > 2019)
                    {
                        bc.Busregnum.Passed = false;
                        bc.Busregnum.Errors = new List<string>() { "Empty or Invalid Busregnum or Tinum" };
                    }
                    else
                    {
                        bc.Busregnum.Passed = true;
                        bc.Busregnum.Errors = null;
                    }

                    var sectorindcode_subsecindcode = stringHelper.Sectorindcode_Subsecindcode(bc.Sectorindcode.Data, bc.Subsecindcode.Data);
                    bc.Sectorindcode.Data = sectorindcode_subsecindcode[0];
                    bc.Subsecindcode.Data = sectorindcode_subsecindcode[1];

                    // Expand known business-name abbreviations (e.g. "ENT" -> "ENTERPRISE",
                    // via the shortForms mapping managed on the Business Names Normalizer
                    // admin page) BEFORE the keyword-validity checks just below --
                    // previously this ran much later in the method, so a name like
                    // "VISION & SPORTS ENT" was checked against the keyword list in its
                    // raw, unexpanded form and failed, with the (too-late) expansion to
                    // "ENTERPRISE" only happening afterward. Admin-managed mappings had
                    // effectively no effect on cleaning as a result.
                    bc.Businessname.Data = ConvertBusinessShortFormToLongForm(bc.Businessname.Data);
                    bc.Tradingname.Data = ConvertBusinessShortFormToLongForm(bc.Tradingname.Data);
                    bc.NameOfComGuarantor1.Data = ConvertBusinessShortFormToLongForm(bc.NameOfComGuarantor1.Data);
                    bc.NameOfComGuarantor2.Data = ConvertBusinessShortFormToLongForm(bc.NameOfComGuarantor2.Data);
                    bc.NameOfComGuarantor3.Data = ConvertBusinessShortFormToLongForm(bc.NameOfComGuarantor3.Data);
                    bc.NameOfComGuarantor4.Data = ConvertBusinessShortFormToLongForm(bc.NameOfComGuarantor4.Data);

                    var bName = FilterBusinessKeywords(bc.Businessname.Data);

                    // EXCEPTION: if this record matched the reference and the name is
                    // consistent with what was previously accepted → trust it (it was
                    // manually reviewed and registered from a prior month's clean),
                    // same mechanism as the IND side.
                    bool businessNameValidatedByReference = false;
                    if (!string.IsNullOrWhiteSpace(bc.ReferenceCanonicalName))
                    {
                        var currentBusinessNameSorted = NormaliseNameSortedTokens(bc.Businessname.Data, string.Empty);
                        if (!string.IsNullOrWhiteSpace(currentBusinessNameSorted) &&
                            (currentBusinessNameSorted == bc.ReferenceCanonicalName ||
                             bc.ReferenceCanonicalName.Contains(currentBusinessNameSorted) ||
                             currentBusinessNameSorted.Contains(bc.ReferenceCanonicalName)))
                        {
                            businessNameValidatedByReference = true;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(bName) && !businessNameValidatedByReference)
                    {
                        bc.Businessname.Passed = false;
                        bc.Businessname.Errors = new List<string>() { " Invalid Business Name " };
                    }
                    else if (businessNameValidatedByReference && string.IsNullOrWhiteSpace(bName))
                    {
                        // Name only passed because of reference trust -- note it in the
                        // WARNING column so associates know why, same as IND.
                        if (string.IsNullOrWhiteSpace(bc.ReferenceNameTrustNote))
                            bc.ReferenceNameTrustNote = "REFERENCE MATCH — NAME ACCEPTED FROM PREVIOUS CLEAN";
                    }

                    var NCG1 = FilterBusinessKeywords(bc.NameOfComGuarantor1.Data);
                    if (string.IsNullOrWhiteSpace(NCG1))
                    {
                        bc.NameOfComGuarantor1.Data = string.Empty;
                        bc.BusRegOfGuarantor1.Data = string.Empty;
                    }
                    else
                    {
                        bc.NameOfComGuarantor1.Data = NCG1;
                    }


                    //NameOfComGuarantor2
                    var NCG2 = FilterBusinessKeywords(bc.NameOfComGuarantor2.Data);
                    if (string.IsNullOrWhiteSpace(NCG2))
                    {
                        bc.NameOfComGuarantor2.Data = string.Empty;
                        bc.Busregofguarantor2.Data = string.Empty;
                    }
                    else
                    {
                        bc.NameOfComGuarantor2.Data = NCG2;
                    }

                    //NameOfComGuarantor3
                    var NCG3 = FilterBusinessKeywords(bc.NameOfComGuarantor3.Data);
                    if (string.IsNullOrWhiteSpace(NCG3))
                    {
                        bc.NameOfComGuarantor3.Data = string.Empty;
                        bc.BusRegOfGuarantor3.Data = string.Empty;
                    }
                    else
                    {
                        bc.NameOfComGuarantor3.Data = NCG3;
                    }

                    //NameOfComGuarantor4
                    var NCG4 = FilterBusinessKeywords(bc.NameOfComGuarantor4.Data);
                    if (string.IsNullOrWhiteSpace(NCG4))
                    {
                        bc.NameOfComGuarantor4.Data = string.Empty;
                        bc.BusRegOfGuarantor4.Data = string.Empty;
                    }
                    else
                    {
                        bc.NameOfComGuarantor4.Data = NCG4;
                    }

                    var bbbbb = bc.Busregnum.Data;
                    //# FINAL BUS IND
                    var busregnum_isGhanacard = stringHelper.IsGhanacardAndNotRepeatingCharcters(bc.Busregnum.Data);
                    var tinum_isGhanacard = stringHelper.IsGhanacardAndNotRepeatingCharcters(bc.Tinum.Data);



                    if (bc.Bustype.Data == "SOLE PROPRIETORSHIP" || bc.Bustype.Data == "A")
                    {
                        // Lock in the value right here, immediately before it can be
                        // cleared below -- regardless of whatever OldData already
                        // holds from upstream processing, this guarantees the last
                        // known value survives for UNL display. The Clean sheet is
                        // unaffected either way (WriteRow<T> only falls back to
                        // OldData when routing to Unloadable, per the "!isSlean"
                        // branch) -- this only matters for what an analyst sees
                        // without needing to open the raw file.
                        if (busregnum_isGhanacard.thisIsAGhanaCard || busregnum_isGhanacard.hasSixUniqueCharactersCharacters)
                            bc.Busregnum.OldData = bc.Busregnum.Data;
                        if (tinum_isGhanacard.thisIsAGhanaCard || tinum_isGhanacard.hasSixUniqueCharactersCharacters)
                            bc.Tinum.OldData = bc.Tinum.Data;

                        //Remove Ghana card
                        if (busregnum_isGhanacard.thisIsAGhanaCard)
                        {
                            bc.Busregnum.Data = string.Empty;
                        }
                        if (tinum_isGhanacard.thisIsAGhanaCard)
                        {
                            bc.Tinum.Data = string.Empty;
                        }

                        //Remove is hasSixUniqueCharacters
                        if (busregnum_isGhanacard.hasSixUniqueCharactersCharacters)
                        {
                            bc.Busregnum.Data = string.Empty;
                        }
                        if (tinum_isGhanacard.hasSixUniqueCharactersCharacters)
                        {
                            bc.Tinum.Data = string.Empty;
                        }

                        if (string.IsNullOrWhiteSpace(bc.Busregnum.Data) && string.IsNullOrWhiteSpace(bc.Tinum.Data) && _date.IsValidFormat && _date.TheDate.Year > 2019)
                        {
                            bc.Busregnum.Passed = false;
                            bc.Busregnum.Errors = new List<string>() { " Busregnum or Tinum: cannot be a Ghana card or contains 6 or more consecutive repeating characters " };
                        }

                    }

                    // ── All-numeric registration number ─────────────────────────────
                    // A registration number OR TIN being purely numeric (no letters --
                    // Ghana business reg numbers and TINs normally carry a letter
                    // prefix, e.g. "CS", "BN", "P") is suspicious on its own, but if
                    // the OTHER identifier is well-formed (has a letter), that's still
                    // a usable, trustworthy ID for this business -- e.g. Busregnum
                    // "12893454" alongside Tinum "P000237846" shouldn't fail just
                    // because the registration number alone looks numeric. Only flag
                    // when NEITHER field has a letter, mirroring the "both missing"
                    // logic of the Ghana-card check above. Same 2019 cutoff
                    // precedent -- only enforced on recent submissions.
                    //
                    // Deliberately OUTSIDE the SOLE PROPRIETORSHIP/"A" check above --
                    // this was previously nested inside it, so it only ever ran for
                    // sole proprietorships. A malformed registration number is
                    // equally suspicious for any business type, not just sole props
                    // (which get the additional, specific Ghana-card check because
                    // that's where a personal ID number is most likely to have been
                    // entered by mistake).
                    var tinumCleanedForNumericCheck = new string(
                        (bc.Tinum.Data ?? string.Empty).Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray());
                    var busregnumCleanedForNumericCheck = new string(
                        (bc.Busregnum.Data ?? string.Empty).Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray());

                    bool busregLooksAllNumeric = !string.IsNullOrWhiteSpace(busregnumCleanedForNumericCheck) &&
                                                  busregnumCleanedForNumericCheck.All(char.IsDigit);
                    bool tinumLooksAllNumeric  = !string.IsNullOrWhiteSpace(tinumCleanedForNumericCheck) &&
                                                  tinumCleanedForNumericCheck.All(char.IsDigit);
                    bool busregHasLetter = !string.IsNullOrWhiteSpace(busregnumCleanedForNumericCheck) && !busregLooksAllNumeric;
                    bool tinumHasLetter  = !string.IsNullOrWhiteSpace(tinumCleanedForNumericCheck) && !tinumLooksAllNumeric;

                    if (!busregHasLetter && !tinumHasLetter && _date.IsValidFormat && _date.TheDate.Year > 2019)
                    {
                        if (busregLooksAllNumeric)
                            AddCellError(bc.Busregnum, "BUSREGNUM: ALL-NUMERIC REGISTRATION NUMBER — PROVIDE A VALID REGISTRATION NUMBER (TINUM ALSO NOT A USABLE ALTERNATE ID)");
                        if (tinumLooksAllNumeric)
                            AddCellError(bc.Tinum, "TINUM: ALL-NUMERIC TIN — PROVIDE A VALID TIN (BUSREGNUM ALSO NOT A USABLE ALTERNATE ID)");
                    }

                    // ── Alpha-only / placeholder values (e.g. "STAFF", "VOTERS", "0000") ──
                    // Mirrors the Individual-side fix for the same problem: a value
                    // with no digit at all, or one made of a single repeated
                    // character, is a category label or placeholder, not a real
                    // registration number or TIN. Flag only when NEITHER field is a
                    // genuinely usable ID -- if the other field is fine, that's a
                    // usable ID for this business and the placeholder elsewhere
                    // shouldn't fail the record (same "either ID is enough"
                    // precedent as the all-numeric check just above).
                    bool busregIsPlaceholder = !string.IsNullOrWhiteSpace(busregnumCleanedForNumericCheck) &&
                        (busregnumCleanedForNumericCheck.Distinct().Count() == 1 || !busregnumCleanedForNumericCheck.Any(char.IsDigit));
                    bool tinumIsPlaceholder = !string.IsNullOrWhiteSpace(tinumCleanedForNumericCheck) &&
                        (tinumCleanedForNumericCheck.Distinct().Count() == 1 || !tinumCleanedForNumericCheck.Any(char.IsDigit));
                    bool busregIsUsable = !string.IsNullOrWhiteSpace(busregnumCleanedForNumericCheck) && !busregIsPlaceholder;
                    bool tinumIsUsable = !string.IsNullOrWhiteSpace(tinumCleanedForNumericCheck) && !tinumIsPlaceholder;

                    if (!busregIsUsable && !tinumIsUsable)
                    {
                        if (busregIsPlaceholder)
                            AddCellError(bc.Busregnum, "BUSREGNUM APPEARS TO BE A PLACEHOLDER OR CATEGORY LABEL, NOT AN ACTUAL REGISTRATION NUMBER — PROVIDE A VALID REGISTRATION NUMBER (TINUM ALSO NOT A USABLE ALTERNATE ID)");
                        if (tinumIsPlaceholder)
                            AddCellError(bc.Tinum, "TINUM APPEARS TO BE A PLACEHOLDER OR CATEGORY LABEL, NOT AN ACTUAL TIN — PROVIDE A VALID TIN (BUSREGNUM ALSO NOT A USABLE ALTERNATE ID)");
                    }
                    // "W" (non-bank) is the Business-side default placeholder, unlike
                    // Individual's "P" -- checking against "P" here meant this never
                    // fired for a bank's raw "W", so banks stayed "W" instead of
                    // being upgraded to "X".
                    if (bc.CreditFacilityType.Data == "W")
                    {
                        bc.CreditFacilityType.Data = isSubscriber_X_or_Y;
                    }



                    var proofOfAddType_proofOfAddNum = stringHelper.ProofOfAddType_ProofOfAddNum(bc.ProofOfAddType.Data, bc.ProofOfAddNum.Data);
                    bc.ProofOfAddType = proofOfAddType_proofOfAddNum[0];
                    bc.ProofOfAddNum = proofOfAddType_proofOfAddNum[1];

                    var turnovercurrency_turnoveramount = stringHelper.TurnoverCurrency_TurnoverAmount(bc.Turnovercurrency.Data, bc.Turnoveramount.Data);
                    bc.Turnovercurrency = turnovercurrency_turnoveramount[0];
                    bc.Turnoveramount = turnovercurrency_turnoveramount[1];



                    var curlocadd = stringHelper.CleanAddresses(bc.Curlocadd1.Data, bc.Curlocadd2.Data, bc.Curlocadd3.Data, bc.Curlocadd4.Data);
                    bc.Curlocadd1 = curlocadd[0];
                    bc.Curlocadd2 = curlocadd[1];
                    bc.Curlocadd3 = curlocadd[2];
                    bc.Curlocadd4 = curlocadd[3];

                    var PostAddrLine = stringHelper.CleanAddresses(bc.PostAddrLine1.Data, bc.PostAddrLine2.Data, bc.PostAddrLine3.Data, bc.PostAddrLine4.Data);
                    bc.PostAddrLine1 = PostAddrLine[0];
                    bc.PostAddrLine2 = PostAddrLine[1];
                    bc.PostAddrLine3 = PostAddrLine[2];
                    bc.PostAddrLine4 = PostAddrLine[3];




                    var Officetel = stringHelper.MobileTel1_MobileTel2(bc.Officetel1.Data.Replace("(", "").Replace(")", "").Replace("'", ""), bc.Officetel2.Data.Replace("(", "").Replace(")", "").Replace("'", ""));
                    bc.Officetel1 = Officetel[0];
                    bc.Officetel2 = Officetel[1];

                    var MobileTel_G1 = stringHelper.MobileTel1_MobileTel2(bc.G1Mobile.Data.Replace("(", "").Replace(")", "").Replace("'", ""), bc.G1HomeTel.Data.Replace("(", "").Replace(")", "").Replace("'", ""));
                    bc.G1Mobile = MobileTel_G1[0];
                    bc.G1HomeTel = MobileTel_G1[1];

                    var MobileTel_G2 = stringHelper.MobileTel1_MobileTel2(bc.G2Mobile.Data.Replace("(", "").Replace(")", "").Replace("'", ""), bc.G2HomeTel.Data.Replace("(", "").Replace(")", "").Replace("'", ""));
                    bc.G2Mobile = MobileTel_G2[0];
                    bc.G2HomeTel = MobileTel_G2[1];

                    var MobileTel_G3 = stringHelper.MobileTel1_MobileTel2(bc.G3Mobile.Data.Replace("(", "").Replace(")", "").Replace("'", ""), bc.G3HomeTel.Data.Replace("(", "").Replace(")", "").Replace("'", ""));
                    bc.G3Mobile = MobileTel_G3[0];
                    bc.G3HomeTel = MobileTel_G3[1];

                    var MobileTel_G4 = stringHelper.MobileTel1_MobileTel2(bc.G4Mobile.Data.Replace("(", "").Replace(")", "").Replace("'", ""), bc.G4HomeTel.Data.Replace("(", "").Replace(")", "").Replace("'", ""));
                    bc.G4Mobile = MobileTel_G4[0];
                    bc.G4HomeTel = MobileTel_G4[1];



                    if (bc.Officetel1.Data == bc.Officetel2.Data) { bc.Officetel2.Data = string.Empty; }


                    var facilityDisbursementAmt = stringHelper.FacilityAmount_DisbursementAmt(bc.FacilityAmount.Data, bc.DisbursementAmt.Data);
                    bc.FacilityAmount = facilityDisbursementAmt[0];
                    bc.DisbursementAmt = facilityDisbursementAmt[1];


                    var CurBal0_AmtOverdue1_WOffAmount2_NDIA3_Fsc4 = stringHelper.CurBal0_AmtOverdue1_WOffAmount2_NDIA3_Fsc4_d_Date5(bc.CurBal.Data, bc.AmountInArrears.Data, bc.WrittenOffAmt.Data, bc.NDIA.Data, bc.FacilityStatusCode.Data, bc.FacilityStatusDate.Data, filename);
                    bc.CurBal = CurBal0_AmtOverdue1_WOffAmount2_NDIA3_Fsc4[0];
                    bc.AmountInArrears = CurBal0_AmtOverdue1_WOffAmount2_NDIA3_Fsc4[1];
                    bc.WrittenOffAmt = CurBal0_AmtOverdue1_WOffAmount2_NDIA3_Fsc4[2];
                    bc.NDIA = CurBal0_AmtOverdue1_WOffAmount2_NDIA3_Fsc4[3];
                    bc.FacilityStatusCode = CurBal0_AmtOverdue1_WOffAmount2_NDIA3_Fsc4[4];
                    bc.FacilityStatusDate = CurBal0_AmtOverdue1_WOffAmount2_NDIA3_Fsc4[5];


                    if (string.IsNullOrWhiteSpace(bc.FacilityStatusCode.Data))
                    {
                        bc.FacilityStatusDate.Data = string.Empty;
                    }


                    var g1Add = stringHelper.CleanAddresses(bc.G1Add1.Data, bc.G1Add2.Data, bc.G1Add3.Data, "");
                    bc.G1Add1 = g1Add[0];
                    bc.G1Add2 = g1Add[1];
                    bc.G1Add3 = g1Add[2];


                    var g2Add = stringHelper.CleanAddresses(bc.G2Add1.Data, bc.G2Add2.Data, bc.G2Add3.Data, "");
                    bc.G2Add1 = g2Add[0];
                    bc.G2Add2 = g2Add[1];
                    bc.G2Add3 = g2Add[2];


                    var g3Add = stringHelper.CleanAddresses(bc.G3Add1.Data, bc.G3Add2.Data, bc.G3Add3.Data, "");
                    bc.G3Add1 = g3Add[0];
                    bc.G3Add2 = g3Add[1];
                    bc.G3Add3 = g3Add[2];


                    var g4Add = stringHelper.CleanAddresses(bc.G4Add1.Data, bc.G4Add2.Data, bc.G4Add3.Data, "");
                    bc.G4Add1 = g4Add[0];
                    bc.G4Add2 = g4Add[1];
                    bc.G4Add3 = g4Add[2];

                    var G1 = GurantorDataConvertor(bc.G1Surname.Data, bc.G1FirstName.Data, bc.G1MiddleNames.Data, bc.G1NatID.Data, bc.G1VotID.Data, bc.G1DrivLic.Data,
              bc.G1PassNum.Data, bc.G1SSN.Data, bc.G1Gender.Data, bc.G1DOB.Data, bc.G1Add1.Data, bc.G1Add2.Data, bc.G1Add3.Data, string.Empty, bc.G1HomeTel.Data,
              bc.G1WorkTel.Data, bc.G1Mobile.Data, "ind");
                    bc.G1Surname.Data = G1.S;
                    bc.G1FirstName.Data = G1.F;
                    bc.G1MiddleNames.Data = G1.M;
                    bc.G1NatID.Data = G1.NID;
                    bc.G1VotID.Data = G1.VID;
                    bc.G1DrivLic.Data = G1.DID;
                    bc.G1PassNum.Data = G1.PID;
                    bc.G1SSN.Data = G1.SSID;
                    bc.G1Gender.Data = G1.GENDER;
                    bc.G1DOB.Data = G1.DOB;
                    bc.G1Add1.Data = G1.ADD1;
                    bc.G1Add2.Data = G1.ADD2;
                    bc.G1Add3.Data = G1.ADD3;
                    bc.G1HomeTel.Data = G1.HTEL;
                    bc.G1WorkTel.Data = G1.WTEL;
                    bc.G1Mobile.Data = G1.GMOB;

                    var G2 = GurantorDataConvertor(bc.G2Surname.Data, bc.G2FirstName.Data, bc.G2MiddleNames.Data, bc.G2NatID.Data, bc.G2VotID.Data, bc.G2DrivLic.Data,
                    bc.G2PassNum.Data, bc.G2SSN.Data, bc.G2Gender.Data, bc.G2DOB.Data, bc.G2Add1.Data, bc.G2Add2.Data, bc.G2Add3.Data, string.Empty, bc.G2HomeTel.Data,
                    bc.G2WorkTel.Data, bc.G2Mobile.Data, "ind");
                    bc.G2Surname.Data = G2.S;
                    bc.G2FirstName.Data = G2.F;
                    bc.G2MiddleNames.Data = G2.M;
                    bc.G2NatID.Data = G2.NID;
                    bc.G2VotID.Data = G2.VID;
                    bc.G2DrivLic.Data = G2.DID;
                    bc.G2PassNum.Data = G2.PID;
                    bc.G2SSN.Data = G2.SSID;
                    bc.G2Gender.Data = G2.GENDER;
                    bc.G2DOB.Data = G2.DOB;
                    bc.G2Add1.Data = G2.ADD1;
                    bc.G2Add2.Data = G2.ADD2;
                    bc.G2Add3.Data = G2.ADD3;
                    bc.G2HomeTel.Data = G2.HTEL;
                    bc.G2WorkTel.Data = G2.WTEL;
                    bc.G2Mobile.Data = G2.GMOB;

                    var G3 = GurantorDataConvertor(bc.G3Surname.Data, bc.G3FirstName.Data, bc.G3MiddleNames.Data, bc.G3NatID.Data, bc.G3VotID.Data, bc.G3DrivLic.Data,
                    bc.G3PassNum.Data, bc.G3SSN.Data, bc.G3Gender.Data, bc.G3DOB.Data, bc.G3Add1.Data, bc.G3Add2.Data, bc.G3Add3.Data, string.Empty, bc.G3HomeTel.Data,
                    bc.G3WorkTel.Data, bc.G3Mobile.Data, "ind");
                    bc.G3Surname.Data = G3.S;
                    bc.G3FirstName.Data = G3.F;
                    bc.G3MiddleNames.Data = G3.M;
                    bc.G3NatID.Data = G3.NID;
                    bc.G3VotID.Data = G3.VID;
                    bc.G3DrivLic.Data = G3.DID;
                    bc.G3PassNum.Data = G3.PID;
                    bc.G3SSN.Data = G3.SSID;
                    bc.G3Gender.Data = G3.GENDER;
                    bc.G3DOB.Data = G3.DOB;
                    bc.G3Add1.Data = G3.ADD1;
                    bc.G3Add2.Data = G3.ADD2;
                    bc.G3Add3.Data = G3.ADD3;
                    bc.G3HomeTel.Data = G3.HTEL;
                    bc.G3WorkTel.Data = G3.WTEL;
                    bc.G3Mobile.Data = G3.GMOB;

                    var G4 = GurantorDataConvertor(bc.G4Surname.Data, bc.G4FirstName.Data, bc.G4MiddleNames.Data, bc.G4NatID.Data, bc.G4VotID.Data, bc.G4DrivLic.Data,
                    bc.G4PassNum.Data, bc.G4SSN.Data, bc.G4Gender.Data, bc.G4DOB.Data, bc.G4Add1.Data, bc.G4Add2.Data, bc.G4Add3.Data, string.Empty, bc.G4HomeTel.Data,
                    bc.G4WorkTel.Data, bc.G4Mobile.Data, "ind");
                    bc.G4Surname.Data = G4.S;
                    bc.G4FirstName.Data = G4.F;
                    bc.G4MiddleNames.Data = G4.M;
                    bc.G4NatID.Data = G4.NID;
                    bc.G4VotID.Data = G4.VID;
                    bc.G4DrivLic.Data = G4.DID;
                    bc.G4PassNum.Data = G4.PID;
                    bc.G4SSN.Data = G4.SSID;
                    bc.G4Gender.Data = G4.GENDER;
                    bc.G4DOB.Data = G4.DOB;
                    bc.G4Add1.Data = G4.ADD1;
                    bc.G4Add2.Data = G4.ADD2;
                    bc.G4Add3.Data = G4.ADD3;
                    bc.G4HomeTel.Data = G4.HTEL;
                    bc.G4WorkTel.Data = G4.WTEL;
                    bc.G4Mobile.Data = G4.GMOB;



                    var ids_G1 = stringHelper.ValidateIDsMain1_G(bc.G1NatID.Data, bc.G1VotID.Data, bc.G1DrivLic.Data, bc.G1PassNum.Data, bc.G1SSN.Data, string.Empty, string.Empty);
                    bc.G1NatID = ids_G1[0];
                    bc.G1VotID = ids_G1[1];
                    bc.G1DrivLic = ids_G1[2];
                    bc.G1PassNum = ids_G1[3];
                    bc.G1SSN = ids_G1[4];

                    var ids_G2 = stringHelper.ValidateIDsMain1_G(bc.G2NatID.Data, bc.G2VotID.Data, bc.G2DrivLic.Data, bc.G2PassNum.Data, bc.G2SSN.Data, string.Empty, string.Empty);
                    bc.G2NatID = ids_G2[0];
                    bc.G2VotID = ids_G2[1];
                    bc.G2DrivLic = ids_G2[2];
                    bc.G2PassNum = ids_G2[3];
                    bc.G2SSN = ids_G2[4];

                    var ids_G3 = stringHelper.ValidateIDsMain1_G(bc.G3NatID.Data, bc.G3VotID.Data, bc.G3DrivLic.Data, bc.G3PassNum.Data, bc.G3SSN.Data, string.Empty, string.Empty);
                    bc.G3NatID = ids_G3[0];
                    bc.G3VotID = ids_G3[1];
                    bc.G3DrivLic = ids_G3[2];
                    bc.G3PassNum = ids_G3[3];
                    bc.G3SSN = ids_G3[4];

                    var ids_G4 = stringHelper.ValidateIDsMain1_G(bc.G4NatID.Data, bc.G4VotID.Data, bc.G4DrivLic.Data, bc.G4PassNum.Data, bc.G4SSN.Data, string.Empty, string.Empty);
                    bc.G4NatID = ids_G4[0];
                    bc.G4VotID = ids_G4[1];
                    bc.G4DrivLic = ids_G4[2];
                    bc.G4PassNum = ids_G4[3];
                    bc.G4SSN = ids_G4[4];

                    bc.G1Gender.Data = string.Empty;
                    bc.G2Gender.Data = string.Empty;
                    bc.G3Gender.Data = string.Empty;
                    bc.G4Gender.Data = string.Empty;

                    var surname_Nature_NameGuarantor1 = Surname_Nature_NameOfComGuarantorNormalizer(bc.G1Surname.Data, bc.NatureOfGuarantor1.Data, bc.NameOfComGuarantor1.Data);
                    bc.G1Surname.Data = surname_Nature_NameGuarantor1._surname;
                    bc.NatureOfGuarantor1.Data = surname_Nature_NameGuarantor1._natureOfComGua;
                    bc.NameOfComGuarantor1.Data = surname_Nature_NameGuarantor1._nameOfComGuaran;


                    var surname_Nature_NameGuarantor2 = Surname_Nature_NameOfComGuarantorNormalizer(bc.G2Surname.Data, bc.NatureOfGuarantor2.Data, bc.NameOfComGuarantor2.Data);
                    bc.G2Surname.Data = surname_Nature_NameGuarantor2._surname;
                    bc.NatureOfGuarantor2.Data = surname_Nature_NameGuarantor2._natureOfComGua;
                    bc.NameOfComGuarantor2.Data = surname_Nature_NameGuarantor2._nameOfComGuaran;


                    var surname_Nature_NameGuarantor3 = Surname_Nature_NameOfComGuarantorNormalizer(bc.G3Surname.Data, bc.NatureOfGuarantor3.Data, bc.NameOfComGuarantor3.Data);
                    bc.G3Surname.Data = surname_Nature_NameGuarantor3._surname;
                    bc.NatureOfGuarantor3.Data = surname_Nature_NameGuarantor3._natureOfComGua;
                    bc.NameOfComGuarantor3.Data = surname_Nature_NameGuarantor3._nameOfComGuaran;


                    var surname_Nature_NameGuarantor4 = Surname_Nature_NameOfComGuarantorNormalizer(bc.G4Surname.Data, bc.NatureOfGuarantor4.Data, bc.NameOfComGuarantor4.Data);
                    bc.G4Surname.Data = surname_Nature_NameGuarantor4._surname;
                    bc.NatureOfGuarantor4.Data = surname_Nature_NameGuarantor4._natureOfComGua;
                    bc.NameOfComGuarantor4.Data = surname_Nature_NameGuarantor4._nameOfComGuaran;

                    var _mDate = stringHelper.CheckDate(bc.MaturityDate.Data);
                    var _dDate = stringHelper.CheckDate(bc.DisbursementDate.Data);
                    var fDate = stringHelper.GetFacilityDateFromFileName(filename, true).LastDate;
                    if ((_mDate.IsValidFormat && _dDate.IsValidFormat) && _mDate.TheDate <= _dDate.TheDate)
                    {
                        bc.DisbursementDate.Passed = false;
                        bc.MaturityDate.Passed = false;
                        bc.MaturityDate.Errors = new List<string>() { "DISBURSEMENT DATE CANNOT BE GREATER THAN MATURITY DATE" };
                        bc.DisbursementDate.Errors = new List<string>() { "DISBURSEMENT DATE CANNOT BE GREATER THAN MATURITY DATE" };
                    }
                    if (_mDate.IsValidFormat && !string.IsNullOrWhiteSpace(stringHelper.DateCompareGreater(_dDate.TheDate, fDate)))
                    {
                        bc.DisbursementDate.Passed = false;
                        bc.DisbursementDate.Errors = new List<string>() { "DISBURSEMENT DATE CANNOT BE GREATER THAN SUBMISSION DATE" };
                    }
                    // ── Implausible year sanity check ────────────────────────────────
                    // CheckDate's TryParseExact("yyyyMMdd") accepts any year .NET's
                    // DateTime supports (0001-9999) -- a garbled value like "12000423"
                    // parses as a "valid" date (23 Apr, year 1200) and passes the
                    // 18-plus/not-future checks trivially. A facility genuinely
                    // disbursed before 1990 wouldn't still be an active, reportable
                    // credit facility today.
                    if (_dDate.IsValidFormat && _dDate.TheDate.Year < 1990)
                    {
                        bc.DisbursementDate.Passed = false;
                        bc.DisbursementDate.Errors = new List<string>() { $"DISBURSEMENT DATE YEAR ({_dDate.TheDate.Year}) IS IMPLAUSIBLY EARLY — CHECK FOR A DATA ENTRY ERROR" };
                    }
                    if (_mDate.IsValidFormat && _mDate.TheDate.Year < 1990)
                    {
                        bc.MaturityDate.Passed = false;
                        bc.MaturityDate.Errors = new List<string>() { $"MATURITY DATE YEAR ({_mDate.TheDate.Year}) IS IMPLAUSIBLY EARLY — CHECK FOR A DATA ENTRY ERROR" };
                    }

                    if (string.IsNullOrWhiteSpace(bc.FacilityStatusCode.Data))
                    {
                        bc.ClosedDate.Data = string.Empty;
                        bc.ClosureReason.Data = string.Empty;
                    }


                    if (bc.FacilityStatusCode.Data != "E" || bc.FacilityStatusCode.Data != "R")
                    {
                        bc.ReasonForRestructure.Data = string.Empty;
                        bc.DateRestructured.Data = string.Empty;
                    }
                    if (bc.FacilityStatusCode.Data != "C" || bc.FacilityStatusCode.Data != "P" || bc.FacilityStatusCode.Data != "T")
                    {
                        bc.ClosureReason.Data = string.Empty;
                        bc.ClosedDate.Data = string.Empty;
                    }

                    if (bc.FacilityStatusCode.Data == "C" || bc.FacilityStatusCode.Data == "P" || bc.FacilityStatusCode.Data == "T")
                    {
                        bc.CurBal.Data = "0";
                    }

                    //if the new data has a zero current balance with status code C and the old data has an empty current balance with status code A , unl
                    if (bc.CurBal.Data == "0" && bc.FacilityStatusCode.Data == "C" && string.IsNullOrWhiteSpace(bc.CurBal.OldData) && bc.FacilityStatusCode.OldData == "A")
                    {
                        bc.CurBal.Passed = false;
                        bc.CurBal.Errors = new List<string>() { "CURRENT BALANCE SHOULD NOT BE EMPTY FOR ACTIVE LOANS" };
                    }
                    if (bc.FacilityStatusCode.Data != "W")
                    {
                        bc.ReasonForWrittenOff.Data = string.Empty;
                    }

                    if (bc != null && bc.DisburStatus != null && bc.DisbursementDate != null)
                    {
                        if (bc.DisburStatus.Length > 6)
                        {
                            bc.DisbursementDate.Passed = false;
                            bc.DisbursementDate.Errors = new List<string>() { bc.DisburStatus };
                        }
                    }
                    if (IsLetter(bc.FacilityStatusCode.Data) || string.IsNullOrWhiteSpace(bc.FacilityStatusCode.Data))
                    {
                        bc.FacilityStatusCode.Passed = true;
                        bc.FacilityStatusCode.Data = bc.FacilityStatusCode.Data;
                    }
                    else
                    {
                        bc.FacilityStatusCode.Passed = false;
                        bc.FacilityStatusCode.Errors = new List<string>() { " Invalid FacilityStatusCode " };
                    }

                    if (
                            (bc.CurBal.Data == "0" || string.IsNullOrWhiteSpace(bc.CurBal.Data)) &&
                            string.IsNullOrWhiteSpace(bc.FacilityStatusCode.Data) &&
                            !string.IsNullOrWhiteSpace(bc.AmountInArrears.Data)
                       )
                    {
                        bc.CurBal.Passed = false;
                        bc.CurBal.Errors = new List<string>() { "CURRENT BALANCE SHOULD NOT BE EMPTY" };
                    }

                    if (!string.IsNullOrWhiteSpace(bc.CustomerIDBusregnumTinumStatus))
                    {
                        // CustomerIDBusregnumTinumStatus now carries the actual
                        // conflict detail (which customer IDs / values), built where
                        // the conflict was detected -- not just a flat flag -- so
                        // this is naming the specific problem, not a generic message.
                        AddCellError(bc.CustomerID, bc.CustomerIDBusregnumTinumStatus);
                    }

                    if (!string.IsNullOrWhiteSpace(bc.DisbursementDateMisMatch))
                    {
                        bc.DisbursementDate.Passed = false;
                        bc.DisbursementDate.Errors = new List<string>() { "DUPLICATE WITH DIFFERENT DISBURSEMENT DATE" };
                    }

                    NormalizeAccountAndCustomerFields(bc.Facilityaccnum, bc.CustomerID, "FACILITYACCNUM");

                    if (!stringHelper.AccountCustomerInValidCharacter(bc.CustomerID.Data))
                    {
                        AddCellError(bc.CustomerID, "CUSTOMERID: CONTAIN INVALID CHARACTERS");
                    }


                    if (!stringHelper.AccountCustomerInValidCharacter(bc.Facilityaccnum.Data))
                    {
                        AddCellError(bc.Facilityaccnum, "FACILITYACCNUM: CONTAIN INVALID CHARACTERS");
                    }
                    if (!stringHelper.AccountCustomerInValidCharacter(bc.Busregnum.Data))
                    {
                        AddCellError(bc.Busregnum, "BUSREGNUM: CONTAIN INVALID CHARACTERS");
                    }
                    if (!stringHelper.AccountCustomerInValidCharacter(bc.Tinum.Data))
                    {
                        AddCellError(bc.Tinum, "TINUM: CONTAIN INVALID CHARACTERS");
                    }



                    bc.CurBal.Data = bc.CurBal.Data.Replace("-", "");

                    bool CreditFacilityAccNum = bc.Facilityaccnum.Errors == null || bc.Facilityaccnum.Errors.Count == 0;
                    bool Tinum = bc.Tinum.Errors == null || bc.Tinum.Errors.Count == 0;
                    bool CustomerID = bc.CustomerID.Errors == null || bc.CustomerID.Errors.Count == 0;
                    bool FacilityAmount = bc.FacilityAmount.Errors == null || bc.FacilityAmount.Errors.Count == 0;
                    bool DisbursementDate = bc.DisbursementDate.Errors == null || bc.DisbursementDate.Errors.Count == 0;
                    bool DisbursementAmt = bc.DisbursementAmt.Errors == null || bc.DisbursementAmt.Errors.Count == 0;
                    bool MaturityDate = bc.MaturityDate.Errors == null || bc.MaturityDate.Errors.Count == 0;
                    bool CurBal = bc.CurBal.Errors == null || bc.CurBal.Errors.Count == 0;
                    bool Busregnumid = bc.Busregnum.Errors == null || bc.Busregnum.Errors.Count == 0;
                    bool BusinessName = bc.Businessname.Errors == null || bc.Businessname.Errors.Count == 0;


                    bc.IsPassed = Tinum && Busregnumid && BusinessName && CreditFacilityAccNum && CustomerID && FacilityAmount && DisbursementDate &&
                    DisbursementAmt && MaturityDate && CurBal;
                    return bc.IsPassed;
    }

    public bool Evaluate(IndividualContext ic)
    {
        if (ic == null) return false;

                    //# FINAL IND
                    ic.Data = "D";
                    ic.NDIA.OldData = ic.UNL_NDIA_OldData;
                    // maintain converted ndias in clean file. Set ndias in unl sheet to the old ndia
                    var ids = stringHelper.ValidateIDsMain1(
                        ic.NatIDNum.Data,
                        ic.VotersIDNum.Data,
                        ic.DriverLicNum.Data,
                        ic.PassportNum.Data,
                        ic.SSNum.Data,
                        ic.OtherIDNum.Data,
                        ic.DisbursementDate.Data);


                    ic.NatIDNum = ids[0];
                    ic.VotersIDNum = ids[1];
                    ic.DriverLicNum = ids[2];
                    ic.PassportNum = ids[3];
                    ic.SSNum = ids[4];
                    ic.OtherIDNum = ids[5];


                    if (ic.NatIDNum.Data.EndsWith("000000"))
                    {
                        ic.NatIDNum.Data = string.Empty;
                    }


                    if (ic.VotersIDNum.Data.EndsWith("000000"))
                    {
                        ic.VotersIDNum.Data = string.Empty;
                    }


                    if (ic.DriverLicNum.Data.EndsWith("000000"))
                    {
                        ic.DriverLicNum.Data = string.Empty;
                    }

                    if (ic.PassportNum.Data.EndsWith("000000"))
                    {
                        ic.PassportNum.Data = string.Empty;
                    }
                    if (ic.SSNum.Data.EndsWith("000000"))
                    {
                        ic.SSNum.Data = string.Empty;
                    }


                    var _date = stringHelper.CheckDate(ic.DisbursementDate.Data);

                    if (string.IsNullOrWhiteSpace(ic.NatIDNum.Data) &&
                        string.IsNullOrWhiteSpace(ic.VotersIDNum.Data) &&
                        string.IsNullOrWhiteSpace(ic.DriverLicNum.Data) &&
                        string.IsNullOrWhiteSpace(ic.PassportNum.Data) &&
                        string.IsNullOrWhiteSpace(ic.SSNum.Data) && _date.IsValidFormat && _date.TheDate.Year > 2019)
                    {
                        ic.NatIDNum.Passed = false;
                        ic.NatIDNum.Errors = new List<string>() { "WRONG OR EMPTY IDS. PROVIDE VALID IDS" };
                    }

                    // ── Sequential placeholder digits (e.g. "123456789", "012345") ──────
                    // Unlike Business registration numbers, an all-numeric personal ID is
                    // normal and acceptable on its own -- Ghana Card numbers aside, many
                    // legitimate ID types are pure digits. What's specifically suspicious
                    // is a STRICTLY ASCENDING digit run, the classic placeholder someone
                    // types when they don't have a real value to enter.
                    static bool IsSequentialPlaceholderDigits(string? value)
                    {
                        if (string.IsNullOrWhiteSpace(value)) return false;
                        var cleaned = new string(value.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray());
                        if (cleaned.Length < 6 || !cleaned.All(char.IsDigit)) return false;
                        for (int i = 1; i < cleaned.Length; i++)
                        {
                            if (cleaned[i] - cleaned[i - 1] != 1) return false;
                        }
                        return true;
                    }
                    const string sequentialIdMsg = "ID APPEARS TO BE A SEQUENTIAL PLACEHOLDER VALUE (e.g. 123456789) — PROVIDE A VALID ID";
                    var allIdFields = new (CellDataAndStatus? field, string label)[]
                    {
                        (ic.NatIDNum, "NATIDNUM"), (ic.VotersIDNum, "VOTERSIDNUM"), (ic.DriverLicNum, "DRIVERLICNUM"),
                        (ic.PassportNum, "PASSPORTNUM"), (ic.SSNum, "SSNUM"), (ic.EzwichNum, "EZWICHNUM"), (ic.OtherIDNum, "OTHERIDNUM"),
                        (ic.TINum, "TINUM")
                    };
                    foreach (var (idField, idLabel) in allIdFields)
                    {
                        if (IsSequentialPlaceholderDigits(idField?.Data))
                        {
                            AddCellError(idField, sequentialIdMsg);
                        }
                    }

                    // ── Alpha-only ID values (e.g. "STAFF", "GHANACARD", "STUDENT") ─────
                    // These are category labels, not real ID numbers -- a genuine ID
                    // always contains at least one digit. People often enter these
                    // meaning "the real ID for this person is a staff/Ghana Card/
                    // student ID", not literally the word itself. Flag as invalid
                    // ONLY when NONE of the 8 ID columns has a genuinely valid-
                    // looking (digit-containing) value -- if even one other column
                    // does, that's a usable ID for this person and the label
                    // elsewhere shouldn't fail the record (same "either ID is
                    // enough" precedent as the Business all-numeric check). This
                    // also matters for the cross-record "same ID, different person"
                    // check: values like these are excluded from that comparison
                    // entirely (see IsValidIDValue) since many unrelated people can
                    // coincidentally share the same label.
                    static bool IsAlphaOnlyIDValue(string? value)
                    {
                        if (string.IsNullOrWhiteSpace(value)) return false;
                        var cleaned = new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
                        return cleaned.Length > 0 && cleaned.All(char.IsLetter);
                    }
                    bool anyIdFieldHasDigit = allIdFields.Any(f => !string.IsNullOrWhiteSpace(f.field?.Data) && f.field!.Data!.Any(char.IsDigit));
                    if (!anyIdFieldHasDigit)
                    {
                        const string alphaOnlyIdMsg = "ID APPEARS TO BE A CATEGORY LABEL (e.g. STAFF, GHANACARD, STUDENT), NOT AN ACTUAL ID NUMBER — PROVIDE A VALID ID (NO OTHER COLUMN HAS A USABLE ID EITHER)";
                        foreach (var (idField, idLabel) in allIdFields)
                        {
                            if (IsAlphaOnlyIDValue(idField?.Data))
                            {
                                AddCellError(idField, alphaOnlyIdMsg);
                            }
                        }
                    }




                    var ids_G1 = stringHelper.ValidateIDsMain1_G(
                       ic.G1NatID.Data,
                       ic.G1VotID.Data,
                       ic.G1DrivLic.Data,
                       ic.G1PassNum.Data,
                       ic.G1SSN.Data,
                       string.Empty,
                       string.Empty);

                    ic.G1NatID = ids_G1[0];
                    ic.G1VotID = ids_G1[1];
                    ic.G1DrivLic = ids_G1[2];
                    ic.G1PassNum = ids_G1[3];
                    ic.G1SSN = ids_G1[4];


                    var ids_G2 = stringHelper.ValidateIDsMain1_G(
                          ic.G2NatID.Data,
                          ic.G2VotID.Data,
                          ic.G2DrivLic.Data,
                          ic.G2PassNum.Data,
                          ic.G2SSN.Data,
                          string.Empty,
                          string.Empty);

                    ic.G2NatID = ids_G2[0];
                    ic.G2VotID = ids_G2[1];
                    ic.G2DrivLic = ids_G2[2];
                    ic.G2PassNum = ids_G2[3];
                    ic.G2SSN = ids_G2[4];


                    var ids_G3 = stringHelper.ValidateIDsMain1_G(
                         ic.G3NatID.Data,
                         ic.G3VotID.Data,
                         ic.G3DrivLic.Data,
                         ic.G3PassNum.Data,
                         ic.G3SSN.Data,
                         string.Empty,
                         string.Empty);

                    ic.G3NatID = ids_G3[0];
                    ic.G3VotID = ids_G3[1];
                    ic.G3DrivLic = ids_G3[2];
                    ic.G3PassNum = ids_G3[3];
                    ic.G3SSN = ids_G3[4];


                    var ids_G4 = stringHelper.ValidateIDsMain1_G(
                         ic.G4NatID.Data,
                         ic.G4VotID.Data,
                         ic.G4DrivLic.Data,
                         ic.G4PassNum.Data,
                         ic.G4SSN.Data,
                         string.Empty,
                         string.Empty);

                    ic.G4NatID = ids_G4[0];
                    ic.G4VotID = ids_G4[1];
                    ic.G4DrivLic = ids_G4[2];
                    ic.G4PassNum = ids_G4[3];
                    ic.G4SSN = ids_G4[4];

                    var otherIdNumb_otherIdType = stringHelper.OtherIdAndOtherIDType(ic.OtherIDNum.Data, ic.OtherIDType.Data);
                    ic.OtherIDNum = otherIdNumb_otherIdType[0];
                    ic.OtherIDType = otherIdNumb_otherIdType[1];

                    var allNames = stringHelper.Surname_FirtName_MiddleName(ic.Surname.Data, ic.FirstName.Data, ic.MiddleNames.Data);
                    ic.Surname = allNames[0];
                    ic.FirstName = allNames[1];
                    ic.MiddleNames = allNames[2];


                    if (ic.Surname.Data.Contains(',') || ic.FirstName.Data.Contains(',') || ic.MiddleNames.Data.Contains(','))
                    {
                        ic.Surname.Passed = false;
                        ic.Surname.Errors = new List<string>() { " NAMES CANNOT CONTAIN SEPECIAL CHARACTER(S) " };
                    }

                    // ── Reference-trust check computed up front ───────────────────────
                    // Moved ahead of ContainsRestrictedWord (below) and the Sprint 8
                    // keyword block further down, so BOTH name-keyword gates respect
                    // it -- previously ContainsRestrictedWord ran before this was
                    // computed, so a trusted name (e.g. "ABED SMART", registered from
                    // a prior month's manually-cleaned CLEAN sheet) could still be
                    // flagged here even though the later check already trusted it.
                    bool nameValidatedByReference = false;
                    if (!string.IsNullOrWhiteSpace(ic.ReferenceCanonicalName))
                    {
                        var currentSortedForReferenceCheck = NormaliseNameSortedTokens(
                            ic.Surname?.Data ?? string.Empty,
                            ic.FirstName?.Data ?? string.Empty);
                        // Trust if current name matches or is a subset of reference name
                        if (!string.IsNullOrWhiteSpace(currentSortedForReferenceCheck) &&
                            (currentSortedForReferenceCheck == ic.ReferenceCanonicalName ||
                             ic.ReferenceCanonicalName.Contains(currentSortedForReferenceCheck) ||
                             currentSortedForReferenceCheck.Contains(ic.ReferenceCanonicalName)))
                        {
                            nameValidatedByReference = true;
                        }
                    }

                    if (!nameValidatedByReference && (ContainsRestrictedWord(ic.Surname.Data) || ContainsRestrictedWord(ic.FirstName.Data) || ContainsRestrictedWord(ic.MiddleNames.Data)))
                    {
                        ic.Surname.Passed = false; ic.FirstName.Passed = false;
                        ic.Surname.Errors = new List<string>() { " SURNAME AND FIRSTNAME ARE MANDATORY AND SHOULD CONTAIN VALID NAMES " };
                    }

                    // ── Sprint 8: Business keyword detection in IND name fields ───────
                    // If any name field contains a business keyword, the record is for
                    // a company not an individual — flag as unloadable.
                    // EXCEPTION: if this record matched the reference and the name is
                    // consistent with what was previously accepted → trust it (it was
                    // manually reviewed and cleaned last month).
                    var combinedName = $"{ic.Surname.Data} {ic.FirstName.Data} {ic.MiddleNames.Data}".Trim();
                    var upperName = combinedName.ToUpperInvariant();

                    if (!nameValidatedByReference)
                    {
                        if (!string.IsNullOrWhiteSpace(FilterBusinessKeywords(combinedName)))
                        {
                            ic.Surname.Passed = false;
                            ic.FirstName.Passed = false;
                            AddCellError(ic.Surname,
                                "INVALID NAME: BUSINESS KEYWORDS DETECTED IN INDIVIDUAL RECORD — USE COMMERCIAL FILE TYPE");
                        }

                        // ── Sprint 8: AND in IND name = multiple persons — unloadable ─
                        if (upperName.Contains(" AND ") || upperName.EndsWith(" AND"))
                        {
                            ic.Surname.Passed = false;
                            ic.FirstName.Passed = false;
                            AddCellError(ic.Surname,
                                "INVALID NAME: MULTIPLE INDIVIDUALS DETECTED — A FACILITY RECORD MUST BE ASSIGNED TO A SINGLE PERSON");
                        }
                    }
                    else
                    {
                        // Name passed by reference — add a note in the WARNING column
                        // so associates know this record was trusted from last month's clean
                        if (string.IsNullOrWhiteSpace(ic.ReferenceNameTrustNote))
                            ic.ReferenceNameTrustNote = "REFERENCE MATCH — NAME ACCEPTED FROM PREVIOUS CLEAN";
                    }

                    var proofOfAddType_proofOfAddNum = stringHelper.ProofOfAddType_ProofOfAddNum(ic.ProofOfAddType.Data, ic.ProofOfAddNum.Data);
                    ic.ProofOfAddType = proofOfAddType_proofOfAddNum[0];
                    ic.ProofOfAddNum = proofOfAddType_proofOfAddNum[1];


                    var curlocadd = stringHelper.CleanAddresses(ic.CurResAddr1.Data, ic.CurResAddr2.Data, ic.CurResAddr3.Data, ic.CurResAddr4.Data);
                    ic.CurResAddr1 = curlocadd[0];
                    ic.CurResAddr2 = curlocadd[1];
                    ic.CurResAddr3 = curlocadd[2];
                    ic.CurResAddr4 = curlocadd[3];


                    var PrevResAddr = stringHelper.CleanAddresses(ic.PrevResAddr1.Data, ic.PrevResAddr2.Data, ic.PrevResAddr3.Data, ic.PrevResAddr4.Data);
                    ic.PrevResAddr1 = PrevResAddr[0];
                    ic.PrevResAddr2 = PrevResAddr[1];
                    ic.PrevResAddr3 = PrevResAddr[2];
                    ic.PrevResAddr4 = PrevResAddr[3];


                    var PostAddrLine = stringHelper.CleanAddresses(ic.PostAddrLine1.Data, ic.PostAddrLine2.Data, ic.PostAddrLine3.Data, ic.PostAddrLine4.Data);
                    ic.PostAddrLine1 = PostAddrLine[0];
                    ic.PostAddrLine2 = PostAddrLine[1];
                    ic.PostAddrLine3 = PostAddrLine[2];
                    ic.PostAddrLine4 = PostAddrLine[3];




                    var MobileTel = stringHelper.MobileTel1_MobileTel2(ic.MobileTel1.Data.Replace("(", "").Replace(")", "").Replace("'", ""), ic.MobileTel2.Data.Replace("(", "").Replace(")", "").Replace("'", ""));
                    ic.MobileTel1 = MobileTel[0];
                    ic.MobileTel2 = MobileTel[1];

                    var MobileTel_G1 = stringHelper.MobileTel1_MobileTel2(ic.G1Mobile.Data.Replace("(", "").Replace(")", "").Replace("'", ""), ic.G1HomeTel.Data.Replace("(", "").Replace(")", "").Replace("'", ""));
                    ic.G1Mobile = MobileTel_G1[0];
                    ic.G1HomeTel = MobileTel_G1[1];

                    var MobileTel_G2 = stringHelper.MobileTel1_MobileTel2(ic.G2Mobile.Data.Replace("(", "").Replace(")", "").Replace("'", ""), ic.G2HomeTel.Data.Replace("(", "").Replace(")", "").Replace("'", ""));
                    ic.G2Mobile = MobileTel_G2[0];
                    ic.G2HomeTel = MobileTel_G2[1];

                    var MobileTel_G3 = stringHelper.MobileTel1_MobileTel2(ic.G3Mobile.Data.Replace("(", "").Replace(")", "").Replace("'", ""), ic.G3HomeTel.Data.Replace("(", "").Replace(")", "").Replace("'", ""));
                    ic.G3Mobile = MobileTel_G3[0];
                    ic.G3HomeTel = MobileTel_G3[1];

                    var MobileTel_G4 = stringHelper.MobileTel1_MobileTel2(ic.G4Mobile.Data.Replace("(", "").Replace(")", "").Replace("'", ""), ic.G4HomeTel.Data.Replace("(", "").Replace(")", "").Replace("'", ""));
                    ic.G4Mobile = MobileTel_G4[0];
                    ic.G4HomeTel = MobileTel_G4[1];



                    if (ic.WorkTel.Data == ic.MobileTel1.Data) { ic.WorkTel.Data = string.Empty; }
                    if (ic.WorkTel.Data == ic.MobileTel2.Data) { ic.WorkTel.Data = string.Empty; }
                    if (ic.WorkTel.Data == ic.HomeTel.Data) { ic.WorkTel.Data = string.Empty; }



                    if (ic.MobileTel1.Data == ic.MobileTel2.Data) { ic.MobileTel2.Data = string.Empty; }
                    if (ic.MobileTel1.Data == ic.HomeTel.Data)
                    {
                        ic.HomeTel.Data = string.Empty;
                    }
                    if (ic.MobileTel2.Data == ic.HomeTel.Data)
                    {
                        ic.HomeTel.Data = string.Empty;
                    }




                    var EmpAddr = stringHelper.CleanAddresses(ic.EmpAddr1.Data, ic.EmpAddr2.Data, ic.EmpAddr3.Data, ic.EmpAddr4.Data);
                    ic.EmpAddr1 = EmpAddr[0];
                    ic.EmpAddr2 = EmpAddr[1];
                    ic.EmpAddr3 = EmpAddr[2];
                    ic.EmpAddr4 = EmpAddr[3];


                    var incomeCurrency_Income_jointOrSoleAcc_NoParticipantsInAcc = stringHelper.IncomeCurrency_Income_jointOrSoleAcc_NoParticipantsInAcc(ic.IncomeCurrency.Data, ic.Income.Data, ic.JointOrSoleAcc.Data, ic.NoParticipantsInAcc.Data);
                    ic.IncomeCurrency = incomeCurrency_Income_jointOrSoleAcc_NoParticipantsInAcc[0];
                    ic.Income = incomeCurrency_Income_jointOrSoleAcc_NoParticipantsInAcc[1];
                    ic.JointOrSoleAcc = incomeCurrency_Income_jointOrSoleAcc_NoParticipantsInAcc[2];
                    ic.NoParticipantsInAcc = incomeCurrency_Income_jointOrSoleAcc_NoParticipantsInAcc[3];


                    var facilityDisbursementAmt = stringHelper.FacilityAmount_DisbursementAmt(ic.FacilityAmount.Data, ic.DisbursementAmt.Data);
                    ic.FacilityAmount = facilityDisbursementAmt[0];
                    ic.DisbursementAmt = facilityDisbursementAmt[1];


                    var CurBal0_AmtOverdue1_WOffAmount2_NDIA3_Fsc4 = stringHelper.CurBal0_AmtOverdue1_WOffAmount2_NDIA3_Fsc4_d_Date5(ic.CurBal.Data, ic.AmountInArrears.Data, ic.WrittenOffAmt.Data, ic.NDIA.Data, ic.FacilityStatusCode.Data, ic.FacilityStatusDate.Data, filename);
                    ic.CurBal = CurBal0_AmtOverdue1_WOffAmount2_NDIA3_Fsc4[0];
                    ic.AmountInArrears = CurBal0_AmtOverdue1_WOffAmount2_NDIA3_Fsc4[1];
                    ic.WrittenOffAmt = CurBal0_AmtOverdue1_WOffAmount2_NDIA3_Fsc4[2];
                    ic.NDIA = CurBal0_AmtOverdue1_WOffAmount2_NDIA3_Fsc4[3];
                    ic.FacilityStatusCode = CurBal0_AmtOverdue1_WOffAmount2_NDIA3_Fsc4[4];
                    ic.FacilityStatusDate = CurBal0_AmtOverdue1_WOffAmount2_NDIA3_Fsc4[5];


                    if (string.IsNullOrWhiteSpace(ic.FacilityStatusCode.Data))
                    {
                        ic.FacilityStatusDate.Data = string.Empty;
                    }


                    var g1Add = stringHelper.CleanAddresses(ic.G1Add1.Data, ic.G1Add2.Data, ic.G1Add3.Data, "");
                    ic.G1Add1 = g1Add[0];
                    ic.G1Add2 = g1Add[1];
                    ic.G1Add3 = g1Add[2];


                    var g2Add = stringHelper.CleanAddresses(ic.G2Add1.Data, ic.G2Add2.Data, ic.G2Add3.Data, "");
                    ic.G2Add1 = g2Add[0];
                    ic.G2Add2 = g2Add[1];
                    ic.G2Add3 = g2Add[2];


                    var g3Add = stringHelper.CleanAddresses(ic.G3Add1.Data, ic.G3Add2.Data, ic.G3Add3.Data, "");
                    ic.G3Add1 = g3Add[0];
                    ic.G3Add2 = g3Add[1];
                    ic.G3Add3 = g3Add[2];


                    var g4Add = stringHelper.CleanAddresses(ic.G4Add1.Data, ic.G4Add2.Data, ic.G4Add3.Data, "");
                    ic.G4Add1 = g4Add[0];
                    ic.G4Add2 = g4Add[1];
                    ic.G4Add3 = g4Add[2];

                    //NameOfComGuarantor
                    // Expand known abbreviations first (see matching fix on the Business
                    // side) -- previously this ran much later (after the keyword check
                    // below), so an unrecognized abbreviation like "ENT" caused
                    // NameOfComGuarantor to be wiped to empty here before it ever got
                    // the chance to expand to "ENTERPRISE".
                    ic.NameOfComGuarantor.Data = ConvertBusinessShortFormToLongForm(ic.NameOfComGuarantor.Data);

                    var NCG1 = FilterBusinessKeywords(ic.NameOfComGuarantor.Data);
                    if (string.IsNullOrWhiteSpace(NCG1))
                    {
                        ic.NameOfComGuarantor.Data = string.Empty;
                        ic.BusRegOfGuarantor.Data = string.Empty;
                    }
                    else
                    {
                        ic.NameOfComGuarantor.Data = NCG1;
                    }


                    //FilterBusinessKeywords return empty if business name does not exits
                    var NCG = FilterBusinessKeywords(ic.NameOfComGuarantor.Data);
                    ic.NameOfComGuarantor.Data = NCG;

                    var G1 = GurantorDataConvertor(ic.G1Surname.Data, ic.G1FirstName.Data, ic.G1MiddleNames.Data, ic.G1NatID.Data, ic.G1VotID.Data, ic.G1DrivLic.Data,
                    ic.G1PassNum.Data, ic.G1SSN.Data, ic.G1Gender.Data, ic.G1DOB.Data, ic.G1Add1.Data, ic.G1Add2.Data, ic.G1Add3.Data, string.Empty, ic.G1HomeTel.Data,
                    ic.G1WorkTel.Data, ic.G1Mobile.Data, string.Empty);
                    ic.G1Surname.Data = G1.S;
                    ic.G1FirstName.Data = G1.F;
                    ic.G1MiddleNames.Data = G1.M;
                    ic.G1NatID.Data = G1.NID;
                    ic.G1VotID.Data = G1.VID;
                    ic.G1DrivLic.Data = G1.DID;
                    ic.G1PassNum.Data = G1.PID;
                    ic.G1SSN.Data = G1.SSID;
                    ic.G1Gender.Data = G1.GENDER;
                    ic.G1DOB.Data = G1.DOB;
                    ic.G1Add1.Data = G1.ADD1;
                    ic.G1Add2.Data = G1.ADD2;
                    ic.G1Add3.Data = G1.ADD3;
                    ic.G1HomeTel.Data = G1.HTEL;
                    ic.G1WorkTel.Data = G1.WTEL;
                    ic.G1Mobile.Data = G1.GMOB;


                    var G2 = GurantorDataConvertor(ic.G2Surname.Data, ic.G2FirstName.Data, ic.G2MiddleNames.Data, ic.G2NatID.Data, ic.G2VotID.Data, ic.G2DrivLic.Data,
                   ic.G2PassNum.Data, ic.G2SSN.Data, ic.G2Gender.Data, ic.G2DOB.Data, ic.G2Add1.Data, ic.G2Add2.Data, ic.G2Add3.Data, string.Empty, ic.G2HomeTel.Data,
                   ic.G2WorkTel.Data, ic.G2Mobile.Data, string.Empty);
                    ic.G2Surname.Data = G2.S;
                    ic.G2FirstName.Data = G2.F;
                    ic.G2MiddleNames.Data = G2.M;
                    ic.G2NatID.Data = G2.NID;
                    ic.G2VotID.Data = G2.VID;
                    ic.G2DrivLic.Data = G2.DID;
                    ic.G2PassNum.Data = G2.PID;
                    ic.G2SSN.Data = G2.SSID;
                    ic.G2Gender.Data = G2.GENDER;
                    ic.G2DOB.Data = G2.DOB;
                    ic.G2Add1.Data = G2.ADD1;
                    ic.G2Add2.Data = G2.ADD2;
                    ic.G2Add3.Data = G2.ADD3;
                    ic.G2HomeTel.Data = G2.HTEL;
                    ic.G2WorkTel.Data = G2.WTEL;
                    ic.G2Mobile.Data = G2.GMOB;


                    var G3 = GurantorDataConvertor(ic.G3Surname.Data, ic.G3FirstName.Data, ic.G3MiddleNames.Data, ic.G3NatID.Data, ic.G3VotID.Data, ic.G3DrivLic.Data,
                   ic.G3PassNum.Data, ic.G3SSN.Data, ic.G3Gender.Data, ic.G3DOB.Data, ic.G3Add1.Data, ic.G3Add2.Data, ic.G3Add3.Data, string.Empty, ic.G3HomeTel.Data,
                   ic.G3WorkTel.Data, ic.G3Mobile.Data, string.Empty);
                    ic.G3Surname.Data = G3.S;
                    ic.G3FirstName.Data = G3.F;
                    ic.G3MiddleNames.Data = G3.M;
                    ic.G3NatID.Data = G3.NID;
                    ic.G3VotID.Data = G3.VID;
                    ic.G3DrivLic.Data = G3.DID;
                    ic.G3PassNum.Data = G3.PID;
                    ic.G3SSN.Data = G3.SSID;
                    ic.G3Gender.Data = G3.GENDER;
                    ic.G3DOB.Data = G3.DOB;
                    ic.G3Add1.Data = G3.ADD1;
                    ic.G3Add2.Data = G3.ADD2;
                    ic.G3Add3.Data = G3.ADD3;
                    ic.G3HomeTel.Data = G3.HTEL;
                    ic.G3WorkTel.Data = G3.WTEL;
                    ic.G3Mobile.Data = G3.GMOB;


                    var G4 = GurantorDataConvertor(ic.G4Surname.Data, ic.G4FirstName.Data, ic.G4MiddleNames.Data, ic.G4NatID.Data, ic.G4VotID.Data, ic.G4DrivLic.Data,
                   ic.G4PassNum.Data, ic.G4SSN.Data, ic.G4Gender.Data, ic.G4DOB.Data, ic.G4Add1.Data, ic.G4Add2.Data, ic.G4Add3.Data, string.Empty, ic.G4HomeTel.Data,
                   ic.G4WorkTel.Data, ic.G4Mobile.Data, string.Empty);
                    ic.G4Surname.Data = G4.S;
                    ic.G4FirstName.Data = G4.F;
                    ic.G4MiddleNames.Data = G4.M;
                    ic.G4NatID.Data = G4.NID;
                    ic.G4VotID.Data = G4.VID;
                    ic.G4DrivLic.Data = G4.DID;
                    ic.G4PassNum.Data = G4.PID;
                    ic.G4SSN.Data = G4.SSID;
                    ic.G4Gender.Data = G4.GENDER;
                    ic.G4DOB.Data = G4.DOB;
                    ic.G4Add1.Data = G4.ADD1;
                    ic.G4Add2.Data = G4.ADD2;
                    ic.G4Add3.Data = G4.ADD3;
                    ic.G4HomeTel.Data = G4.HTEL;
                    ic.G4WorkTel.Data = G4.WTEL;
                    ic.G4Mobile.Data = G4.GMOB;



                    var _mDate = stringHelper.CheckDate(ic.MaturityDate.Data);
                    var _dDate = stringHelper.CheckDate(ic.DisbursementDate.Data);
                    var fDate = stringHelper.GetFacilityDateFromFileName(filename, true).LastDate;
                    if ((_mDate.IsValidFormat && _dDate.IsValidFormat) && _mDate.TheDate <= _dDate.TheDate)
                    {
                        ic.DisbursementDate.Passed = false;
                        ic.MaturityDate.Passed = false;
                        ic.MaturityDate.Errors = new List<string>() { "DISBURSEMENT DATE CANNOT BE GREATER THAN MATURITY DATE" };
                        ic.DisbursementDate.Errors = new List<string>() { "DISBURSEMENT DATE CANNOT BE GREATER THAN MATURITY DATE" };
                    }
                    if (_mDate.IsValidFormat && !string.IsNullOrWhiteSpace(stringHelper.DateCompareGreater(_dDate.TheDate, fDate)))
                    {
                        ic.DisbursementDate.Passed = false;
                        ic.DisbursementDate.Errors = new List<string>() { "DISBURSEMENT DATE CANNOT BE GREATER THAN SUBMISSION DATE" };
                    }
                    // ── Implausible year sanity check (see Business-side twin above) ──
                    if (_dDate.IsValidFormat && _dDate.TheDate.Year < 1990)
                    {
                        ic.DisbursementDate.Passed = false;
                        ic.DisbursementDate.Errors = new List<string>() { $"DISBURSEMENT DATE YEAR ({_dDate.TheDate.Year}) IS IMPLAUSIBLY EARLY — CHECK FOR A DATA ENTRY ERROR" };
                    }
                    if (_mDate.IsValidFormat && _mDate.TheDate.Year < 1990)
                    {
                        ic.MaturityDate.Passed = false;
                        ic.MaturityDate.Errors = new List<string>() { $"MATURITY DATE YEAR ({_mDate.TheDate.Year}) IS IMPLAUSIBLY EARLY — CHECK FOR A DATA ENTRY ERROR" };
                    }

                    if (string.IsNullOrWhiteSpace(ic.FacilityStatusCode.Data))
                    {
                        ic.ClosedDate.Data = string.Empty;
                        ic.ClosureReason.Data = string.Empty;
                    }


                    if (ic.FacilityStatusCode.Data != "E" || ic.FacilityStatusCode.Data != "R")
                    {
                        ic.ReasonForRestructure.Data = string.Empty;
                        ic.DateRestructured.Data = string.Empty;
                    }
                    if (ic.FacilityStatusCode.Data != "C" || ic.FacilityStatusCode.Data != "P" || ic.FacilityStatusCode.Data != "T")
                    {
                        ic.ClosureReason.Data = string.Empty;
                        ic.ClosedDate.Data = string.Empty;
                    }

                    if (ic.FacilityStatusCode.Data == "C" || ic.FacilityStatusCode.Data == "P" || ic.FacilityStatusCode.Data == "T")
                    {
                        ic.CurBal.Data = "0";
                    }

                    //if the new data has a zero current balance with status code C and the old data has an empty current balance with status code A , unl
                    if (ic.CurBal.Data == "0" && ic.FacilityStatusCode.Data == "C" && string.IsNullOrWhiteSpace(ic.CurBal.OldData) && ic.FacilityStatusCode.OldData == "A")
                    {
                        ic.CurBal.Passed = false;
                        ic.CurBal.Errors = new List<string>() { "CURRENT BALANCE SHOULD NOT BE EMPTY FOR ACTIVE LOANS" };
                    }
                    if (ic.FacilityStatusCode.Data != "W")
                    {
                        ic.ReasonForWrittenOff.Data = string.Empty;
                    }
                    // NOTE: the old bare "DATE OF BIRTH DOES NOT MATCH PREVIOUS
                    // SUBMISSION" check that used to live here (gated on ic.Status)
                    // has been removed -- it was redundant with the #14 cross-record
                    // DOB check further down, which reliably catches every case this
                    // covered (via the virtual reference records injected earlier)
                    // and produces a more informative message. Having both meant
                    // some records showed the same fact twice, pipe-joined, with the
                    // second copy adding no new information.


                    var surname_Nature_NameGuarantor = Surname_Nature_NameOfComGuarantorNormalizer(ic.G1Surname.Data, ic.NatureOfGuarantor.Data, ic.NameOfComGuarantor.Data);
                    ic.G1Surname.Data = surname_Nature_NameGuarantor._surname;
                    ic.NatureOfGuarantor.Data = surname_Nature_NameGuarantor._natureOfComGua;
                    ic.NameOfComGuarantor.Data = surname_Nature_NameGuarantor._nameOfComGuaran;

                    //BMMATCHING

                    if (ic != null && ic.DisburStatus != null && ic.DisbursementDate != null)
                    {
                        if (ic.DisburStatus.Length > 8)
                        {
                            ic.DisbursementDate.Passed = false;
                            ic.DisbursementDate.Errors = new List<string>() { ic.DisburStatus };
                        }
                    }

                    if (IsLetter(ic.FacilityStatusCode.Data) || string.IsNullOrWhiteSpace(ic.FacilityStatusCode.Data))
                    {
                        ic.FacilityStatusCode.Passed = true;
                        ic.FacilityStatusCode.Data = ic.FacilityStatusCode.Data;
                    }
                    else
                    {
                        ic.FacilityStatusCode.Passed = false;
                        ic.FacilityStatusCode.Errors = new List<string>() { " Invalid FacilityStatusCode " };
                    }


                    if (!string.IsNullOrWhiteSpace(ic.CusumerIDDateOfBirth))
                    {
                        ic.DateOfBirth.Passed = false;
                        ic.DateOfBirth.Errors = new List<string>() { "DUPLICATE WITH DIFFERENT DATE OF BIRTH" };
                    }

                    if (!string.IsNullOrWhiteSpace(ic.DuplicateDoBIssues))
                    {
                        ic.DateOfBirth.Passed = false;
                        ic.DateOfBirth.Errors = new List<string>() { "DUPLICATE WITH SAME OR DIFFERENT DATE OF BIRTH" };
                    }
                    if (!string.IsNullOrWhiteSpace(ic.DisbursementDateMisMatch))
                    {
                        ic.DisbursementDate.Passed = false;
                        ic.DisbursementDate.Errors = new List<string>() { "DUPLICATE WITH SAME OR DIFFERENT DATE OF DISBURSEMENTDATE" };
                    }

                    // ── Sprint 3 cross-record error consumption ───────────────────────
                    // #12 — Same PersonalID value linked to different CustomerIDs
                    // ── #12: NatID/PersonalID conflict consumer ──────────────────────────
                    if (!string.IsNullOrWhiteSpace(ic.SamePersonalIDDifferentCustomer))
                    {
                        var parts   = ic.SamePersonalIDDifferentCustomer.Split(':');
                        var prefix  = parts[0]; // W, E
                        var reason  = parts.Length > 1 ? parts[1] : string.Empty; // DOB, NAME, MISSPELL
                        var fields  = parts.Length > 2 ? parts[2] : (parts.Length > 1 && reason != "DOB" && reason != "NAME" && reason != "MISSPELL" ? reason : "PERSONAL ID");
                        var idField = fields;

                        if (prefix == "W")
                        {
                            // Same person, different CustomerIDs — CLEAN with warning.
                            // "FORMATTING" is a distinct sub-case: names only matched
                            // once hyphen/apostrophe/comma formatting was accounted
                            // for -- worth a different note than the generic
                            // "shared across customer IDs" duplicate-ID warning.
                            var warningMsg = reason == "FORMATTING"
                                ? $"{fields} SHARED ACROSS CUSTOMER IDS — NAMES MATCH ONCE HYPHEN/APOSTROPHE/COMMA FORMATTING IS ACCOUNTED FOR, NOT TREATED AS A MISMATCH"
                                : $"POSSIBLE DUPLICATE CUSTOMER ID — SAME PERSON: {fields} IS SHARED ACROSS MULTIPLE CUSTOMER IDS";
                            ic.ReferenceNameTrustNote = string.IsNullOrWhiteSpace(ic.ReferenceNameTrustNote)
                                ? warningMsg : ic.ReferenceNameTrustNote + " | " + warningMsg;
                        }
                        else if (reason == "DOB")
                            AddCellError(ic.NatIDNum,
                                $"NATIONAL ID LINKED TO DIFFERENT DATES OF BIRTH — SAME {fields} CANNOT BELONG TO TWO DIFFERENT PEOPLE");
                        else if (reason == "MISSPELL")
                            AddCellError(ic.NatIDNum,
                                $"NATIONAL ID CONFLICT: {fields} — NAMES PARTIALLY MATCH, POSSIBLE MISSPELLING — MANUAL REVIEW REQUIRED");
                        else if (ic.SamePersonalIDDifferentCustomer.StartsWith("P"))
                        {
                            AddCellError(ic.CustomerID,
                                stringHelper.GetSamePersonalIDPartialMismatchError(idField));
                        }
                    }

                    // ── #14: Same CustomerID different DOB ────────────────────────────
                    if (!string.IsNullOrWhiteSpace(ic.SameCustomerIDDifferentDOB))
                    {
                        var currentDob = (ic.DateOfBirth?.Data ?? string.Empty).Trim();
                        bool isReferenceMismatch = !string.IsNullOrWhiteSpace(ic.PreviousDOBOnFile)
                            && !string.Equals(ic.PreviousDOBOnFile, currentDob, StringComparison.Ordinal);

                        if (isReferenceMismatch)
                        {
                            var periodNote = string.IsNullOrWhiteSpace(ic.PreviousDOBReportingPeriod)
                                ? string.Empty
                                : $" (LAST CONFIRMED: {ic.PreviousDOBReportingPeriod.ToUpperInvariant()})";
                            AddCellError(ic.DateOfBirth,
                                $"DATE OF BIRTH DOES NOT MATCH PREVIOUS SUBMISSION — WAS {ic.PreviousDOBOnFile}{periodNote}");
                        }
                        else
                        {
                            // Two records within THIS file disagree, with no reference
                            // history to point to -- can't say what it "was", just that
                            // it's inconsistent right now.
                            AddCellError(ic.DateOfBirth,
                                "CUSTOMER ID CONFLICT: SAME CUSTOMER ID IS LINKED TO DIFFERENT DATES OF BIRTH");
                        }
                    }

                    // ── NatID format check ────────────────────────────────────────────
                    if (!string.IsNullOrWhiteSpace(ic.NatIDFormatInvalid))
                        AddCellError(ic.NatIDNum,
                            "INVALID NATIONAL ID FORMAT — EXPECTED 3-LETTER COUNTRY CODE (GHA/NGA/CIV) FOLLOWED BY 10 ALPHANUMERIC CHARACTERS");

                    // ── #15: Same CustomerID — different GHA (Ghana Card) numbers ─────
                    if (!string.IsNullOrWhiteSpace(ic.SameCustomerIDDifferentGhanaCard))
                    {
                        const string ghaMsg = "CUSTOMER ID CONFLICT: SAME CUSTOMER ID IS LINKED TO DIFFERENT GHANA CARD (GHA) NUMBERS";
                        AddCellError(ic.CustomerID, ghaMsg);
                        var ghaParts = ic.SameCustomerIDDifferentGhanaCard.Split(':');
                        var ghaFields = ghaParts.Length > 1 ? ghaParts[1] : string.Empty;
                        foreach (var f in ghaFields.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        {
                            switch (f)
                            {
                                case "NatIDNum": AddCellError(ic.NatIDNum, ghaMsg); break;
                                case "VotersIDNum": AddCellError(ic.VotersIDNum, ghaMsg); break;
                                case "DriverLicNum": AddCellError(ic.DriverLicNum, ghaMsg); break;
                                case "PassportNum": AddCellError(ic.PassportNum, ghaMsg); break;
                                case "SSNum": AddCellError(ic.SSNum, ghaMsg); break;
                                case "EzwichNum": AddCellError(ic.EzwichNum, ghaMsg); break;
                                case "OtherIDNum": AddCellError(ic.OtherIDNum, ghaMsg); break;
                            }
                        }
                    }

                    // #14 — Same CustomerID linked to different names
                    if (!string.IsNullOrWhiteSpace(ic.SameCustomerIDDifferentName) &&
                        ic.SameCustomerIDDifferentName.StartsWith("E"))
                    {
                        var flagParts = ic.SameCustomerIDDifferentName.Split(new[] { "||" }, StringSplitOptions.None);
                        var flagCode = flagParts[0];
                        var custIdInConflict = flagParts.Length > 1 ? flagParts[1] : string.Empty;
                        var namesInConflict = flagParts.Length > 2 ? flagParts[2] : string.Empty;
                        bool misspell14 = flagCode.Contains("MISSPELL");
                        string prevNameNote14 = !string.IsNullOrWhiteSpace(ic.PreviousNameOnFile)
                            ? $" — PREVIOUS NAME ON FILE: {ic.PreviousNameOnFile}"
                            : string.Empty;
                        string nameListNote14 = !string.IsNullOrWhiteSpace(namesInConflict)
                            ? $" — NAMES ON FILE: {namesInConflict}"
                            : string.Empty;
                        string custIdNote14 = !string.IsNullOrWhiteSpace(custIdInConflict)
                            ? $" (CUSTOMER ID {custIdInConflict})"
                            : string.Empty;
                        string msg = misspell14
                            ? $"CUSTOMER ID CONFLICT{custIdNote14}: NAME PARTIALLY MATCHES — POSSIBLE MISSPELLING — MANUAL REVIEW REQUIRED{prevNameNote14}{nameListNote14}"
                            : $"CUSTOMER ID CONFLICT{custIdNote14}: SAME CUSTOMER ID IS LINKED TO DIFFERENT NAMES ACROSS RECORDS{prevNameNote14}{nameListNote14} — VERIFY WHICH IS CORRECT";
                        AddCellError(ic.CustomerID, msg);
                        AddCellError(ic.Surname, msg);
                    }

                    // #2 — Duplicate AccNum+CustomerID pair with different names (exact name mismatch)
                    if (!string.IsNullOrWhiteSpace(ic.DuplicateNameMismatch))
                    {
                        AddCellError(ic.Surname,
                            "CROSS-RECORD CONFLICT: DUPLICATE RECORD HAS A DIFFERENT NAME — REVIEW REQUIRED");
                        AddCellError(ic.FirstName,
                            "CROSS-RECORD CONFLICT: DUPLICATE RECORD HAS A DIFFERENT NAME — REVIEW REQUIRED");
                    }

                    NormalizeAccountAndCustomerFields(ic.CreditFacilityAccNum, ic.CustomerID, "CREDITFACILITYACCNUM");
                    if (!stringHelper.AccountCustomerInValidCharacter(ic.CustomerID.Data))
                    {
                        AddCellError(ic.CustomerID, "CUSTOMERID: CONTAIN INVALID CHARACTERS");
                    }
                    if (!stringHelper.AccountCustomerInValidCharacter(ic.CreditFacilityAccNum.Data))
                    {
                        AddCellError(ic.CreditFacilityAccNum, "CREDITFACILITYACCNUM: CONTAIN INVALID CHARACTERS");
                    }


                    bool CreditFacilityAccNum = ic.CreditFacilityAccNum.Errors == null || ic.CreditFacilityAccNum.Errors.Count == 0;
                    bool Nat_id = ic.NatIDNum.Errors == null || ic.NatIDNum.Errors.Count == 0;
                    bool CustomerID = ic.CustomerID.Errors == null || ic.CustomerID.Errors.Count == 0;
                    bool fNames = ic.FirstName.Errors == null || ic.FirstName.Errors.Count == 0;
                    bool sNames = ic.Surname.Errors == null || ic.Surname.Errors.Count == 0;
                    bool DateOfBirth = ic.DateOfBirth.Errors == null || ic.DateOfBirth.Errors.Count == 0;
                    bool FacilityAmount = ic.FacilityAmount.Errors == null || ic.FacilityAmount.Errors.Count == 0;
                    bool DisbursementDate = ic.DisbursementDate.Errors == null || ic.DisbursementDate.Errors.Count == 0;
                    bool DisbursementAmt = ic.DisbursementAmt.Errors == null || ic.DisbursementAmt.Errors.Count == 0;
                    bool MaturityDate = ic.MaturityDate.Errors == null || ic.MaturityDate.Errors.Count == 0;
                    bool CurBal = ic.CurBal.Errors == null || ic.CurBal.Errors.Count == 0;
                    // ── Previously missing from IsPassed entirely ───────────────────────
                    // VotersIDNum/DriverLicNum/PassportNum/SSNum/EzwichNum/OtherIDNum/TINum
                    // could all have AddCellError called on them (GHA-format checks,
                    // the sequential-placeholder check above, cross-record conflict
                    // checks) and it had zero effect on whether the record passed --
                    // only NatIDNum's error status was ever consulted below. A
                    // sequential placeholder like "123456789" in VotersIDNum, for
                    // example, got visibly flagged but never actually failed the record.
                    bool VotersID = ic.VotersIDNum.Errors == null || ic.VotersIDNum.Errors.Count == 0;
                    bool DriverLic = ic.DriverLicNum.Errors == null || ic.DriverLicNum.Errors.Count == 0;
                    bool PassportN = ic.PassportNum.Errors == null || ic.PassportNum.Errors.Count == 0;
                    bool SSN = ic.SSNum.Errors == null || ic.SSNum.Errors.Count == 0;
                    bool EzwichN = ic.EzwichNum.Errors == null || ic.EzwichNum.Errors.Count == 0;
                    bool OtherIDN = ic.OtherIDNum.Errors == null || ic.OtherIDNum.Errors.Count == 0;
                    bool TINumOk = ic.TINum.Errors == null || ic.TINum.Errors.Count == 0;



                    ic.IsPassed = CreditFacilityAccNum && CustomerID && ids[0].Passed && Nat_id && ic.FirstName.Passed && ic.Surname.Passed && DateOfBirth && FacilityAmount && DisbursementDate &&
                    DisbursementAmt && MaturityDate && CurBal && VotersID && DriverLic && PassportN && SSN && EzwichN && OtherIDN && TINumOk;
                    return ic.IsPassed;
    }

        string NormalizeCustomerId(string? value)
        {
            var normalized = RemoveAllWhitespace(value).ToUpperInvariant();
            if (normalized.All(char.IsDigit))
            {
                normalized = normalized.TrimStart('0');
            }
            if (string.IsNullOrEmpty(normalized) && !string.IsNullOrWhiteSpace(value) && RemoveAllWhitespace(value).All(char.IsDigit))
            {
                return "0";
            }
            return normalized;
        }
        string CleanCustomerIdForOutput(string? value)
        {
            return RemoveAllWhitespace(value).ToUpperInvariant();
        }
        string NormalizeAccountNumber(string? value)
        {
            return RemoveAllWhitespace(value).ToUpperInvariant();
        }
        string RemoveAllWhitespace(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }
        bool HasInternalWhitespace(string? value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            var trimmed = value.Trim();
            return trimmed.Any(char.IsWhiteSpace);
        }
        bool LooksLikeExponentNotation(string? value)
        {
            var normalized = RemoveAllWhitespace(value);
            if (string.IsNullOrWhiteSpace(normalized)) return false;
            return System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[+-]?\d+(\.\d+)?[eE][+-]?\d+$");
        }
        void NormalizeAccountAndCustomerFields(CellDataAndStatus? account, CellDataAndStatus? customer, string accountLabel)
        {
            if (account != null)
            {
                if (string.IsNullOrWhiteSpace(account.Data))
                {
                    AddCellError(account, $"{accountLabel}: MISSING FACILITY ACCOUNT NUMBER");
                }
                else if (LooksLikeExponentNotation(account.Data))
                {
                    AddCellError(account, $"{accountLabel}: INVALID ACCOUNT NUMBER - EXPONENTIATED VALUE DETECTED");
                }
                else
                {
                    if (HasInternalWhitespace(account.Data))
                        account.Data = NormalizeAccountNumber(account.Data);
                    else
                        account.Data = account.Data?.Trim();
                }
            }
            if (customer != null)
            {
                if (LooksLikeExponentNotation(customer.Data))
                    AddCellError(customer, "CUSTOMERID: INVALID CUSTOMER ID - EXPONENTIATED VALUE DETECTED");
                else
                    customer.Data = CleanCustomerIdForOutput(customer.Data);
            }
            if (account != null && customer != null &&
                !string.IsNullOrWhiteSpace(account.Data) &&
                !string.IsNullOrWhiteSpace(customer.Data) &&
                NormalizeAccountNumber(account.Data) == NormalizeCustomerId(customer.Data))
            {
                AddCellError(account, $"{accountLabel}: FACILITY ACCOUNT NUMBER CANNOT BE THE SAME AS CUSTOMERID");
                AddCellError(customer, "CUSTOMERID: CUSTOMERID CANNOT BE THE SAME AS FACILITY ACCOUNT NUMBER");
            }
        }

        string NormaliseNameForComparison(string surname, string firstName)
        {
            var raw = $"{surname} {firstName}".ToUpperInvariant();
            return new string(raw.Where(char.IsLetter).ToArray());
        }

        string[] NormaliseNameTokens(string surname, string firstName)
        {
            var raw = $"{surname} {firstName}".ToUpperInvariant();
            return raw.Split(new[] { ' ', '-', '\'', '.', ',' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(t => new string(t.Where(char.IsLetter).ToArray()))
                      .Where(t => t.Length > 0)
                      .ToArray();
        }

        // Returns sorted-token canonical for reordering detection.
        // "ASIEDU ASARE" and "ASARE ASIEDU" both → "ASAREASIEDU" after sort
        string NormaliseNameSortedTokens(string surname, string firstName)
        {
            var tokens = NormaliseNameTokens(surname, firstName)
                            .OrderBy(t => t);
            return string.Concat(tokens);
        }


        void AddCellError(CellDataAndStatus? cell, string error)
        {
            if (cell == null)
            {
                return;
            }
            cell.Passed = false;
            cell.Errors ??= new List<string>();
            if (!cell.Errors.Contains(error))
            {
                cell.Errors.Add(error);
            }
        }

        bool IsLetter(string input)
        {
            // Check for null, empty string, or length not equal to 1
            if (string.IsNullOrEmpty(input) || input.Length != 1)
            {
                return false;
            }

            // Check if the single character is a letter
            return char.IsLetter(input[0]);
        }
        string ConvertBusinessShortFormToLongForm(string input)
        {
            input = input.Trim().Replace(".", " ").Replace(",", " ").ToUpper();
            var bbbb = shortForms;
            var tokens = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tokens.Length; i++)
            {
                if (shortForms.TryGetValue(tokens[i], out var expanded))
                {
                    tokens[i] = expanded;
                }
            }
            return string.Join(" ", tokens);
        }
        (string _surname, string _natureOfComGua, string _nameOfComGuaran) Surname_Nature_NameOfComGuarantorNormalizer(string surname, string natureOfComGua, string nameOfComGuaran)
        {
            string _surname = surname; string _natureOfComGua = natureOfComGua; string _nameOfComGuaran = nameOfComGuaran;
            if (!string.IsNullOrWhiteSpace(nameOfComGuaran) && string.IsNullOrWhiteSpace(surname))
            {
                natureOfComGua = "102";
                return (surname, natureOfComGua, nameOfComGuaran);
            }
            if (natureOfComGua == "101" && !string.IsNullOrWhiteSpace(surname))
            {
                nameOfComGuaran = string.Empty;
                return (surname, natureOfComGua, nameOfComGuaran);
            }
            if ((!string.IsNullOrWhiteSpace(nameOfComGuaran) || !string.IsNullOrWhiteSpace(surname)) || natureOfComGua == "103")
            {
                natureOfComGua = "101";
            }

            return (surname, natureOfComGua, nameOfComGuaran);
        }
        (string S, string F, string M, string NID, string VID, string DID, string PID, string SSID, string GENDER, string DOB, string ADD1, string ADD2, string ADD3, string ADD4, string HTEL, string WTEL, string GMOB) GurantorDataConvertor(string S, string F, string M, string NID, string VID, string DID, string PID, string SSID, string GENDER, string DOB, string ADD1, string ADD2, string ADD3, string ADD4, string HTEL, string WTEL, string GMOB, string ind1_bus2)
        {
            string _S = S; string _F = F; string _M = M; string _NID = NID; string _VID = VID; string _DID = DID; string _PID = PID; string _SSID = SSID; string _GENDER = GENDER; string _DOB = DOB; string _ADD1 = ADD1; string _ADD2 = ADD2; string _ADD3 = ADD3; string _ADD4 = ADD4; string _HTEL = HTEL; string _WTEL = WTEL; string _GMOB = GMOB;
            var g = stringHelper.Surname_FirtName_MiddleName_Garantor(S, F, M); _S = g[0].Data; _F = g[1].Data; _M = g[2].Data;



            //IT is null
            if (string.IsNullOrWhiteSpace(_S) || string.IsNullOrWhiteSpace(_F))
            {
                _S = string.Empty;
                _F = string.Empty;
                _M = string.Empty;
                _NID = string.Empty;
                _VID = string.Empty;
                _DID = string.Empty;
                _PID = string.Empty;
                _SSID = string.Empty;
                _GENDER = string.Empty;
                _DOB = string.Empty;
                _ADD1 = string.Empty;
                _ADD2 = string.Empty;
                _ADD3 = string.Empty;
                _ADD4 = string.Empty;
                _HTEL = string.Empty;
                _WTEL = string.Empty;
                _GMOB = string.Empty;
                return (_S, _F, _M, _NID, _VID, _DID, _PID, _SSID, _GENDER, _DOB, _ADD1, _ADD2, _ADD3, _ADD4, _HTEL, _WTEL, _GMOB);
            }
            if (ind1_bus2 == "ind")
            {
                //(Business)Contains businssname
                var G1S = FilterBusinessKeywords(_S);
                var G1F = FilterBusinessKeywords(_F);
                var G1M = FilterBusinessKeywords(_M);
                if (!string.IsNullOrWhiteSpace(G1S) || !string.IsNullOrWhiteSpace(G1F) || !string.IsNullOrWhiteSpace(G1M))
                {
                    _S = string.Empty;
                    _F = string.Empty;
                    _M = string.Empty;
                    _NID = string.Empty;
                    _VID = string.Empty;
                    _DID = string.Empty;
                    _PID = string.Empty;
                    _SSID = string.Empty;
                    _GENDER = string.Empty;
                    _DOB = string.Empty;
                    _ADD1 = string.Empty;
                    _ADD2 = string.Empty;
                    _ADD3 = string.Empty;
                    _ADD4 = string.Empty;
                    _HTEL = string.Empty;
                    _WTEL = string.Empty;
                    _GMOB = string.Empty;
                }
                return (_S, _F, _M, _NID, _VID, _DID, _PID, _SSID, _GENDER, _DOB, _ADD1, _ADD2, _ADD3, _ADD4, _HTEL, _WTEL, _GMOB);
            }
            if (string.IsNullOrWhiteSpace(ind1_bus2))
            {
                var vfg1 = ContainsRestrictedWord(_S);
                var vfg2 = ContainsRestrictedWord(_F);
                var vfg3 = ContainsRestrictedWord(_M);
                var vfg4 = ContainsRestrictedWord(_S);
                if (ContainsRestrictedWord(_S) || ContainsRestrictedWord(_F) || ContainsRestrictedWord(_M))
                {
                    _S = string.Empty;
                    _F = string.Empty;
                    _M = string.Empty;
                    _NID = string.Empty;
                    _VID = string.Empty;
                    _DID = string.Empty;
                    _PID = string.Empty;
                    _SSID = string.Empty;
                    _GENDER = string.Empty;
                    _DOB = string.Empty;
                    _ADD1 = string.Empty;
                    _ADD2 = string.Empty;
                    _ADD3 = string.Empty;
                    _ADD4 = string.Empty;
                    _HTEL = string.Empty;
                    _WTEL = string.Empty;
                    _GMOB = string.Empty;
                }
                return (_S, _F, _M, _NID, _VID, _DID, _PID, _SSID, _GENDER, _DOB, _ADD1, _ADD2, _ADD3, _ADD4, _HTEL, _WTEL, _GMOB);
            }
            return (string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }
        string FilterBusinessKeywords(string input)
        {
            // Early exit if input is null, empty, or whitespace
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Convert input to uppercase for case-insensitive comparison
            string inputUpper = input.ToUpper();

            // Split input into words using simple delimiters (spaces, tabs, etc.)
            char[] delimiters = { ' ', '\t', '\n', '\r' }; // Common whitespace characters
            string[] words = inputUpper.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            // Keep only words that exist in BusinessKeyNames
            var filteredWords = new List<string>();
            foreach (var word in words)
            {
                if (settingDataSettings.Contains(word))
                {
                    filteredWords.Add(word);
                }
            }

            // Special handling for multi-word entries like "& CO" and "& SONS"
            if (inputUpper.Contains("& CO"))
            {
                filteredWords.Add("& CO");
            }
            if (inputUpper.Contains("& SONS"))
            {
                filteredWords.Add("& SONS");
            }

            // If no valid keywords found, return an empty string
            if (filteredWords.Count > 0)
            {
                return inputUpper; // Return the original input in uppercase
            }
            else
            {
                return string.Empty;
            }
        }
        string RemoveBusinessKeywords(string input)
        {
            foreach (string keyword in settingDataSettings)
            {
                input = input.Replace(keyword, "", StringComparison.OrdinalIgnoreCase);
            }

            return input;
        }
        bool ContainsRestrictedWord(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Split by common word separators (space, comma, period, hyphen, etc.)
            var words = input.ToUpper().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return words.Any(word => settingDataSettings.Contains(word));
        }
}

