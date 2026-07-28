using CleanserBlazorUI.Entities;

namespace CleanserBlazorUI.Services
{
    /// <summary>
    /// Builds the unloadable log workbook after a cleaning run: the section-1
    /// header row plus the Demographic/Financial rejection breakdown tables.
    ///
    /// Scope note: generates a single-file workbook for the file just
    /// processed (one header row + its two breakdown tables). It does not
    /// append to an existing master log workbook -- if you're tracking a
    /// running log across files, copy this row in for now. Flag if you'd
    /// rather have it read-modify-write an existing master log file instead.
    /// </summary>
    public static class UnloadableLogService
    {
        // ── Demographic vs Financial field split ──────────────────────────
        // Best-guess split from field order in IndividualContext (identity/
        // address/employment fields = Demographic; facility/payment/security/
        // guarantor fields = Financial). NOT yet checked against the BoG
        // field-mapping document from earlier work -- confirm/correct once
        // you see a real log rendered.
        private static readonly HashSet<string> DemographicFieldNames = new()
        {
            "CreditFacilityAccNum", "CustomerID", "BranchCode", "NatIDNum", "VotersIDNum",
            "DriverLicNum", "PassportNum", "SSNum", "EzwichNum", "OtherIDType", "OtherIDNum",
            "TINum", "Gender", "MaritalStatus", "Nationality", "DateOfBirth", "Title", "Surname",
            "FirstName", "MiddleNames", "PreviousNames", "Alias", "ProofOfAddType", "ProofOfAddNum",
            "CurResAddr1", "CurResAddr2", "CurResAddr3", "CurResAddr4", "CurResAddrPostalCode",
            "DateMovedCurrRes", "PrevResAddr1", "PrevResAddr2", "PrevResAddr3", "PrevResAddr4",
            "PrevResAddrPostalCode", "OwnerOrTenant", "PostAddrLine1", "PostAddrLine2",
            "PostAddrLine3", "PostAddrLine4", "PostalAddPostCode", "EmailAddress", "HomeTel",
            "MobileTel1", "MobileTel2", "WorkTel", "NumOfDependants", "EmpType", "EmpPayrollNum",
            "Paypoint", "EmpName", "EmpAddr1", "EmpAddr2", "EmpAddr3", "EmpAddr4",
            "EmpAddrPostalCode", "DateOfEmp", "Occupation", "IncomeCurrency", "Income",
            "JointOrSoleAcc", "NoParticipantsInAcc"
        };
        // Everything else on the record (OldCustomerID onward: facility, payment,
        // arrears, security, guarantor fields) is treated as Financial by default.

        public class FieldRejectionSummary
        {
            public string FieldName { get; set; } = string.Empty;
            public string DescriptionOfErrors { get; set; } = string.Empty;
            public int VolumeAffected { get; set; }
            public double Percentage { get; set; } // fraction 0..1, of UNL records only
        }

        public class UnloadableLogHeader
        {
            public int SerialNo { get; set; }
            public string SubscriberName { get; set; } = string.Empty;
            public string Subcode { get; set; } = string.Empty;
            public string InstitutionType { get; set; } = string.Empty;
            public string Associate { get; set; } = string.Empty;
            public string Filename { get; set; } = string.Empty;
            public int NumberOfRecords { get; set; }
            public string ReportingPeriod { get; set; } = string.Empty;
            public string ReportingYear { get; set; } = string.Empty;
            public string UnloadableReason { get; set; } = string.Empty;
            public string DateEmailed { get; set; } = string.Empty;
            public string DateFixed { get; set; } = string.Empty;
            public string Comments { get; set; } = string.Empty;
            public string DataType { get; set; } = string.Empty;
            public string Months { get; set; } = string.Empty;
            public string LogYear { get; set; } = string.Empty;
        }

        /// <summary>
        /// Aggregates field-level rejections across the UNL batch for one section
        /// (Demographic or Financial), via reflection over CellDataAndStatus
        /// properties -- works for any T shaped that way (Individual or Business).
        /// </summary>
        public static List<FieldRejectionSummary> SummarizeRejections<T>(List<T> unlRecords, bool demographicSection)
        {
            var results = new List<FieldRejectionSummary>();
            if (unlRecords == null || unlRecords.Count == 0) return results;

            var props = typeof(T).GetProperties()
                .Where(p => p.PropertyType == typeof(CellDataAndStatus))
                .ToList();

            int total = unlRecords.Count;

            foreach (var prop in props)
            {
                bool isDemographic = DemographicFieldNames.Contains(prop.Name);
                if (isDemographic != demographicSection) continue;

                int affected = 0;
                var errorMessages = new HashSet<string>();

                foreach (var record in unlRecords)
                {
                    if (record == null) continue;
                    var cell = prop.GetValue(record) as CellDataAndStatus;
                    if (cell == null || cell.Passed) continue;

                    affected++;
                    if (cell.Errors != null)
                    {
                        foreach (var e in cell.Errors)
                        {
                            if (!string.IsNullOrWhiteSpace(e))
                                errorMessages.Add(e.Trim());
                        }
                    }
                }

                if (affected > 0)
                {
                    results.Add(new FieldRejectionSummary
                    {
                        FieldName = prop.Name,
                        DescriptionOfErrors = string.Join("; ", errorMessages.OrderBy(m => m)),
                        VolumeAffected = affected,
                        Percentage = total > 0 ? (double)affected / total : 0
                    });
                }
            }

            return results.OrderByDescending(r => r.VolumeAffected).ToList();
        }

        /// <summary>
        /// One consolidated "Unloadable Reason" string for the header row --
        /// distinct error messages across the whole batch, alphabetized.
        /// </summary>
        public static string BuildUnloadableReasonSummary<T>(List<T> unlRecords)
        {
            if (unlRecords == null || unlRecords.Count == 0) return string.Empty;

            var props = typeof(T).GetProperties()
                .Where(p => p.PropertyType == typeof(CellDataAndStatus))
                .ToList();

            var reasons = new HashSet<string>();
            foreach (var record in unlRecords)
            {
                if (record == null) continue;
                foreach (var prop in props)
                {
                    var cell = prop.GetValue(record) as CellDataAndStatus;
                    if (cell != null && !cell.Passed && cell.Errors != null)
                    {
                        foreach (var e in cell.Errors)
                        {
                            if (!string.IsNullOrWhiteSpace(e)) reasons.Add(e.Trim());
                        }
                    }
                }
            }
            return string.Join("; ", reasons.OrderBy(r => r));
        }

        public static byte[] GenerateWorkbook<T>(List<T> unlRecords, UnloadableLogHeader header)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Unloadable Log");

            int row = 1;

            // ── Section 1: header row ──────────────────────────────────────
            string[] headerCols =
            {
                "Serial No", "Subscriber Name", "Subcode", "Institution type", "Associate",
                "Filename", "Number Of Records", "Reporting Period", "ReportingYear",
                "Unloadable Reason", "Date Emailed", "Date Fixed", "Comments", "DataType",
                "Months", "LogYear"
            };
            for (int c = 0; c < headerCols.Length; c++)
            {
                var cell = ws.Cell(row, c + 1);
                cell.Value = headerCols[c];
                cell.Style.Font.Bold = true;
            }
            row++;

            ws.Cell(row, 1).Value = header.SerialNo;
            ws.Cell(row, 2).Value = header.SubscriberName;
            ws.Cell(row, 3).Value = header.Subcode;
            ws.Cell(row, 4).Value = header.InstitutionType;
            ws.Cell(row, 5).Value = header.Associate;
            ws.Cell(row, 6).Value = header.Filename;
            ws.Cell(row, 7).Value = header.NumberOfRecords;
            ws.Cell(row, 8).Value = header.ReportingPeriod;
            ws.Cell(row, 9).Value = header.ReportingYear;
            ws.Cell(row, 10).Value = header.UnloadableReason;
            ws.Cell(row, 11).Value = header.DateEmailed;
            ws.Cell(row, 12).Value = header.DateFixed;
            ws.Cell(row, 13).Value = header.Comments;
            ws.Cell(row, 14).Value = header.DataType;
            ws.Cell(row, 15).Value = header.Months;
            ws.Cell(row, 16).Value = header.LogYear;
            row += 2;

            row = WriteRejectionSection(ws, row, "2. Demographic Information", "2.1 Rejections",
                SummarizeRejections(unlRecords, demographicSection: true));
            row += 1;
            row = WriteRejectionSection(ws, row, "3. Financial Information", "2.1 Rejections",
                SummarizeRejections(unlRecords, demographicSection: false));

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static int WriteRejectionSection(IXLWorksheet ws, int row, string sectionTitle,
            string subTitle, List<FieldRejectionSummary> items)
        {
            ws.Cell(row, 1).Value = sectionTitle;
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;

            ws.Cell(row, 1).Value = subTitle;
            row++;

            string[] cols = { "Field Name", "Description of Errors", "Volume of Records Affected", "Percentage" };
            for (int c = 0; c < cols.Length; c++)
            {
                var cell = ws.Cell(row, c + 1);
                cell.Value = cols[c];
                cell.Style.Font.Bold = true;
            }
            row++;

            foreach (var item in items)
            {
                ws.Cell(row, 1).Value = item.FieldName;
                ws.Cell(row, 2).Value = item.DescriptionOfErrors;
                ws.Cell(row, 3).Value = item.VolumeAffected;

                var pctCell = ws.Cell(row, 4);
                pctCell.Value = item.Percentage;
                pctCell.Style.NumberFormat.Format = "0.0%";

                row++;
            }

            if (items.Count == 0)
            {
                ws.Cell(row, 1).Value = "(no rejections in this section)";
                row++;
            }

            return row;
        }
    }
}
