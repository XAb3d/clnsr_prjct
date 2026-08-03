using CleanserBlazorUI.Entities;

namespace CleanserBlazorUI.Services
{
    /// <summary>
    /// Builds the unloadable log workbook after a cleaning run: the section-1
    /// header row, a Table 1 breakdown by exact error message, and a Table 2
    /// rollup of those same messages into Demographic / Financial /
    /// Facility &amp; Submission categories.
    ///
    /// Scope note: generates a single-file workbook for the file just
    /// processed. It does not append to an existing master log workbook --
    /// if you're tracking a running log across files, copy this row in for
    /// now.
    /// </summary>
    public static class UnloadableLogService
    {
        // ── Message → category mapping ────────────────────────────────────────
        // Confirmed against a manual audit of every UNL/error message the
        // codebase actually generates (see chat history for the audit). Not
        // meant to be exhaustive forever -- add a new rule here whenever a new
        // message shows up. Anything unmatched falls into "Uncategorized"
        // rather than silently disappearing from Table 2.
        //
        // Order matters: more specific rules must come before broader ones
        // they could otherwise be swallowed by. In particular, the "cannot
        // belong to two different people" rule (dual-category: ID Number +
        // Date of Birth) must be checked before the plain "different dates of
        // birth" rule, since the former message contains the latter phrase.
        private static readonly List<(string KeyPhrase, (string Category, string SubCategory)[] Targets)> MessageCategoryRules = new()
        {
            // ── Dual-category: counts toward BOTH ID Number and Date of Birth ──
            ("CANNOT BELONG TO TWO DIFFERENT PEOPLE", new[]
            {
                ("Demographic", "ID Number"),
                ("Demographic", "Date of Birth"),
            }),

            // ── Demographic: Customer Name ──────────────────────────────────
            ("SURNAME AND FIRSTNAME ARE MANDATORY", new[] { ("Demographic", "Customer Name") }),
            ("NAMES CANNOT CONTAIN SEPECIAL CHARACTER", new[] { ("Demographic", "Customer Name") }),
            ("NAMES CANNOT CONTAIN SPECIAL CHARACTER", new[] { ("Demographic", "Customer Name") }),
            ("Invalid Business Name", new[] { ("Demographic", "Customer Name") }),
            ("BUSINESS KEYWORDS DETECTED", new[] { ("Demographic", "Customer Name") }),
            ("MULTIPLE INDIVIDUALS DETECTED", new[] { ("Demographic", "Customer Name") }),
            ("NAME PARTIALLY MATCHES", new[] { ("Demographic", "Customer Name") }),
            ("NAMES PARTIALLY MATCH", new[] { ("Demographic", "Customer Name") }),
            ("LINKED TO DIFFERENT NAMES ACROSS RECORDS", new[] { ("Demographic", "Customer Name") }),
            ("DUPLICATE RECORD HAS A DIFFERENT NAME", new[] { ("Demographic", "Customer Name") }),

            // ── Demographic: Date of Birth ──────────────────────────────────
            ("DIFFERENT DATES OF BIRTH", new[] { ("Demographic", "Date of Birth") }),
            ("DATE OF BIRTH DOES NOT MATCH PREVIOUS SUBMISSION", new[] { ("Demographic", "Date of Birth") }),
            ("DUPLICATE WITH DIFFERENT DATE OF BIRTH", new[] { ("Demographic", "Date of Birth") }),
            ("DUPLICATE WITH SAME OR DIFFERENT DATE OF BIRTH", new[] { ("Demographic", "Date of Birth") }),

            // ── Demographic: ID Number (GH Card + existing ID types) ────────
            ("WRONG OR EMPTY IDS", new[] { ("Demographic", "ID Number") }),
            ("DIFFERENT GHANA CARD", new[] { ("Demographic", "ID Number") }),
            ("INVALID NATIONAL ID FORMAT", new[] { ("Demographic", "ID Number") }),
            ("SEQUENTIAL PLACEHOLDER VALUE", new[] { ("Demographic", "ID Number") }),

            // ── Demographic: Business Registration Number / TIN ─────────────
            ("Empty or Invalid Busregnum or Tinum", new[] { ("Demographic", "Business Registration Number / TIN") }),
            ("BUSREGNUM: CONTAIN INVALID CHARACTERS", new[] { ("Demographic", "Business Registration Number / TIN") }),
            ("TINUM: CONTAIN INVALID CHARACTERS", new[] { ("Demographic", "Business Registration Number / TIN") }),
            ("cannot be a Ghana card or contains", new[] { ("Demographic", "Business Registration Number / TIN") }),
            ("SAME BUSINESS REGISTRATION NUMBER OR TIN", new[] { ("Demographic", "Business Registration Number / TIN") }),
            ("ALL-NUMERIC REGISTRATION NUMBER", new[] { ("Demographic", "Business Registration Number / TIN") }),

            // ── Financial: Facility Account Number ──────────────────────────
            ("CREDITFACILITYACCNUM: CONTAIN INVALID CHARACTERS", new[] { ("Financial", "Facility Account Number") }),
            ("FACILITYACCNUM: CONTAIN INVALID CHARACTERS", new[] { ("Financial", "Facility Account Number") }),
            ("FACILITY ACCOUNT NUMBER CANNOT BE THE SAME AS CUSTOMERID", new[] { ("Financial", "Facility Account Number") }),
            ("INVALID ACCOUNT NUMBER - EXPONENTIATED", new[] { ("Financial", "Facility Account Number") }),
            ("MISSING FACILITY ACCOUNT NUMBER", new[] { ("Financial", "Facility Account Number") }),

            // ── Financial: Customer ID/Number ───────────────────────────────
            ("CUSTOMERID: CONTAIN INVALID CHARACTERS", new[] { ("Financial", "Customer ID/Number") }),
            ("CUSTOMERID CANNOT BE THE SAME AS FACILITY ACCOUNT NUMBER", new[] { ("Financial", "Customer ID/Number") }),
            ("INVALID CUSTOMER ID - EXPONENTIATED", new[] { ("Financial", "Customer ID/Number") }),

            // ── Financial: Loan/Disbursement Amount ─────────────────────────
            ("FACILITYAMOUNT, DISBURSEMENTAMT CANNOT BE EMPTY OR ZERO", new[] { ("Financial", "Loan/Disbursement Amount") }),

            // ── Financial: Current Balance / Arrears (combined -- these ─────
            // checks validate current balance, amount in arrears, written-off
            // amount, and NDIA jointly; splitting them across separate
            // category rows would misrepresent what the check actually does.
            ("CURRENT BALANCE", new[] { ("Financial", "Current Balance / Arrears") }),
            ("CURRENTBALANCE", new[] { ("Financial", "Current Balance / Arrears") }),
            ("AMOUNT IN ARREARS", new[] { ("Financial", "Current Balance / Arrears") }),
            ("WRITTENOFFAMOUNT", new[] { ("Financial", "Current Balance / Arrears") }),
            ("WRITTEN OF AMOUNT", new[] { ("Financial", "Current Balance / Arrears") }),

            // ── Facility & Submission ────────────────────────────────────────
            ("Invalid FacilityStatusCode", new[] { ("FacilitySubmission", "Facility Status") }),
            ("DISBURSEMENT DATE CANNOT BE GREATER THAN MATURITY DATE", new[] { ("FacilitySubmission", "Disbursement Date") }),
            ("DISBURSEMENT DATE CANNOT BE GREATER THAN SUBMISSION DATE", new[] { ("FacilitySubmission", "Disbursement Date") }),
            ("DUPLICATE WITH SAME OR DIFFERENT DATE OF DISBURSEMENTDATE", new[] { ("FacilitySubmission", "Disbursement Date") }),
            ("DisbursementDate is greater than the reporting period", new[] { ("FacilitySubmission", "Disbursement Date") }),
        };

        private static readonly (string Category, string SubCategory)[] UncategorizedTarget =
        {
            ("Uncategorized", "Uncategorized")
        };

        /// <summary>
        /// Returns every (Category, SubCategory) this message counts toward --
        /// more than one entry only for the deliberate dual-category rule
        /// above. Falls back to "Uncategorized" rather than silently dropping
        /// an unmapped message.
        /// </summary>
        public static (string Category, string SubCategory)[] CategorizeMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return UncategorizedTarget;
            foreach (var rule in MessageCategoryRules)
            {
                if (message.Contains(rule.KeyPhrase, StringComparison.OrdinalIgnoreCase))
                {
                    return rule.Targets;
                }
            }
            return UncategorizedTarget;
        }

        public class MessageRejectionSummary
        {
            public string ErrorMessage { get; set; } = string.Empty;
            public int Count { get; set; }
            public double Percentage { get; set; }
            public string Category { get; set; } = string.Empty;
        }

        public class CategoryRejectionSummary
        {
            public string SubCategory { get; set; } = string.Empty;
            public string DescriptionOfErrors { get; set; } = string.Empty;
            public int VolumeAffected { get; set; }
            public double Percentage { get; set; }
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

        private static List<PropertyInfoAndType> GetCellProps<T>()
        {
            return typeof(T).GetProperties()
                .Where(p => p.PropertyType == typeof(CellDataAndStatus))
                .Select(p => new PropertyInfoAndType(p))
                .ToList();
        }

        private sealed class PropertyInfoAndType
        {
            public System.Reflection.PropertyInfo Prop { get; }
            public PropertyInfoAndType(System.Reflection.PropertyInfo prop) { Prop = prop; }
        }

        /// <summary>
        /// Table 1: distinct error-message breakdown across the UNL batch,
        /// via reflection over CellDataAndStatus properties -- works for any
        /// T shaped that way (Individual or Business). Counts DISTINCT
        /// RECORDS affected by each message, not raw occurrences -- a message
        /// landing on two different fields of the same record (e.g. a joint
        /// validation) still only counts that record once.
        ///
        /// Duplicates is added as its own row afterward if duplicateCount > 0
        /// -- it isn't discoverable by scanning UNL records at all, since
        /// duplicate records live in a separate list entirely. Its percentage
        /// is measured against totalRecordsInFile, not the UNL count, since
        /// it's not a UNL-population statistic.
        /// </summary>
        public static List<MessageRejectionSummary> SummarizeByMessage<T>(
            List<T> unlRecords, int duplicateCount, int totalRecordsInFile)
        {
            var results = new List<MessageRejectionSummary>();
            int unlTotal = unlRecords?.Count ?? 0;

            if (unlRecords != null && unlTotal > 0)
            {
                var props = GetCellProps<T>();
                var messageToRecords = new Dictionary<string, HashSet<object>>();

                foreach (var record in unlRecords)
                {
                    if (record == null) continue;
                    foreach (var p in props)
                    {
                        var cell = p.Prop.GetValue(record) as CellDataAndStatus;
                        if (cell == null || cell.Passed || cell.Errors == null) continue;
                        foreach (var e in cell.Errors)
                        {
                            if (string.IsNullOrWhiteSpace(e)) continue;
                            var trimmed = e.Trim();
                            if (!messageToRecords.TryGetValue(trimmed, out var set))
                            {
                                set = new HashSet<object>();
                                messageToRecords[trimmed] = set;
                            }
                            set.Add(record);
                        }
                    }
                }

                foreach (var kvp in messageToRecords)
                {
                    var targets = CategorizeMessage(kvp.Key);
                    results.Add(new MessageRejectionSummary
                    {
                        ErrorMessage = kvp.Key,
                        Count = kvp.Value.Count,
                        Percentage = unlTotal > 0 ? (double)kvp.Value.Count / unlTotal : 0,
                        Category = string.Join(" + ", targets.Select(t => t.SubCategory))
                    });
                }
            }

            if (duplicateCount > 0)
            {
                results.Add(new MessageRejectionSummary
                {
                    ErrorMessage = "DUPLICATE RECORD (same account/customer/date submitted more than once)",
                    Count = duplicateCount,
                    Percentage = totalRecordsInFile > 0 ? (double)duplicateCount / totalRecordsInFile : 0,
                    Category = "Duplicates *"
                });
            }

            return results.OrderByDescending(r => r.Count).ToList();
        }

        /// <summary>
        /// Table 2: rolls Table 1's message-level data up into
        /// Demographic / Financial / Facility &amp; Submission sub-category
        /// buckets, for one top-level category at a time. Duplicates never
        /// appears here -- it isn't a field-level defect the way everything
        /// else in this table is, and it already has its place in Table 1.
        /// Facility &amp; Submission's Duplicates line item, when it needs to
        /// show up, is added directly by the caller from the same
        /// duplicateCount used in Table 1, not rediscovered here.
        /// </summary>
        public static List<CategoryRejectionSummary> SummarizeByCategory<T>(
            List<T> unlRecords, string topLevelCategory, int duplicateCountForFacilitySubmission = 0)
        {
            var results = new List<CategoryRejectionSummary>();
            int total = unlRecords?.Count ?? 0;

            if (unlRecords != null && total > 0)
            {
                var props = GetCellProps<T>();
                var subcatToRecords = new Dictionary<string, HashSet<object>>();
                var subcatToMessages = new Dictionary<string, HashSet<string>>();

                foreach (var record in unlRecords)
                {
                    if (record == null) continue;
                    foreach (var p in props)
                    {
                        var cell = p.Prop.GetValue(record) as CellDataAndStatus;
                        if (cell == null || cell.Passed || cell.Errors == null) continue;
                        foreach (var e in cell.Errors)
                        {
                            if (string.IsNullOrWhiteSpace(e)) continue;
                            var trimmed = e.Trim();
                            var targets = CategorizeMessage(trimmed);
                            foreach (var (cat, subcat) in targets)
                            {
                                if (cat != topLevelCategory) continue;
                                if (!subcatToRecords.TryGetValue(subcat, out var recSet))
                                {
                                    recSet = new HashSet<object>();
                                    subcatToRecords[subcat] = recSet;
                                }
                                recSet.Add(record);

                                if (!subcatToMessages.TryGetValue(subcat, out var msgSet))
                                {
                                    msgSet = new HashSet<string>();
                                    subcatToMessages[subcat] = msgSet;
                                }
                                msgSet.Add(trimmed);
                            }
                        }
                    }
                }

                foreach (var subcat in subcatToRecords.Keys)
                {
                    results.Add(new CategoryRejectionSummary
                    {
                        SubCategory = subcat,
                        DescriptionOfErrors = string.Join("; ", subcatToMessages[subcat].OrderBy(m => m)),
                        VolumeAffected = subcatToRecords[subcat].Count,
                        Percentage = total > 0 ? (double)subcatToRecords[subcat].Count / total : 0
                    });
                }
            }

            if (topLevelCategory == "FacilitySubmission" && duplicateCountForFacilitySubmission > 0)
            {
                results.Add(new CategoryRejectionSummary
                {
                    SubCategory = "Duplicates",
                    DescriptionOfErrors = "DUPLICATE RECORD (same account/customer/date submitted more than once)",
                    VolumeAffected = duplicateCountForFacilitySubmission,
                    Percentage = 0 // see Table 1 for this figure -- percentage-of-what differs from every other row here
                });
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

            var props = GetCellProps<T>();
            var reasons = new HashSet<string>();
            foreach (var record in unlRecords)
            {
                if (record == null) continue;
                foreach (var p in props)
                {
                    var cell = p.Prop.GetValue(record) as CellDataAndStatus;
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

        public static byte[] GenerateWorkbook<T>(
            List<T> unlRecords, UnloadableLogHeader header, int duplicateCount = 0, int totalRecordsInFile = 0)
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

            // ── Table 1: by exact error message ──────────────────────────────
            row = WriteMessageSection(ws, row, "2. Error Message Breakdown",
                SummarizeByMessage(unlRecords, duplicateCount, totalRecordsInFile));
            row += 1;

            // ── Table 2: rolled up into categories ───────────────────────────
            row = WriteCategorySection(ws, row, "3. Demographic Information", "3.1 Rejections",
                SummarizeByCategory(unlRecords, "Demographic"));
            row += 1;
            row = WriteCategorySection(ws, row, "4. Financial Information", "4.1 Rejections",
                SummarizeByCategory(unlRecords, "Financial"));
            row += 1;
            row = WriteCategorySection(ws, row, "5. Facility & Submission Information", "5.1 Rejections",
                SummarizeByCategory(unlRecords, "FacilitySubmission", duplicateCount));

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static int WriteMessageSection(IXLWorksheet ws, int row, string sectionTitle,
            List<MessageRejectionSummary> items)
        {
            ws.Cell(row, 1).Value = sectionTitle;
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;

            string[] cols = { "Error Message", "Count", "Percentage", "Category" };
            for (int c = 0; c < cols.Length; c++)
            {
                var cell = ws.Cell(row, c + 1);
                cell.Value = cols[c];
                cell.Style.Font.Bold = true;
            }
            row++;

            foreach (var item in items)
            {
                ws.Cell(row, 1).Value = item.ErrorMessage;
                ws.Cell(row, 2).Value = item.Count;

                var pctCell = ws.Cell(row, 3);
                pctCell.Value = item.Percentage;
                pctCell.Style.NumberFormat.Format = "0.0%";

                ws.Cell(row, 4).Value = item.Category;
                row++;
            }

            if (items.Count == 0)
            {
                ws.Cell(row, 1).Value = "(no unloadable records in this run)";
                row++;
            }
            else if (items.Any(i => i.Category == "Duplicates *"))
            {
                ws.Cell(row, 1).Value = "* Duplicates' percentage is of total records in the file — every other row's percentage is of UNL records only.";
                ws.Cell(row, 1).Style.Font.Italic = true;
                row++;
            }

            return row;
        }

        private static int WriteCategorySection(IXLWorksheet ws, int row, string sectionTitle,
            string subTitle, List<CategoryRejectionSummary> items)
        {
            ws.Cell(row, 1).Value = sectionTitle;
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;

            ws.Cell(row, 1).Value = subTitle;
            row++;

            string[] cols = { "Category", "Description of Errors", "Volume of Records Affected", "Percentage" };
            for (int c = 0; c < cols.Length; c++)
            {
                var cell = ws.Cell(row, c + 1);
                cell.Value = cols[c];
                cell.Style.Font.Bold = true;
            }
            row++;

            foreach (var item in items)
            {
                ws.Cell(row, 1).Value = item.SubCategory;
                ws.Cell(row, 2).Value = item.DescriptionOfErrors;
                ws.Cell(row, 3).Value = item.VolumeAffected;

                var pctCell = ws.Cell(row, 4);
                if (item.SubCategory == "Duplicates")
                {
                    ws.Cell(row, 4).Value = "see Table 1";
                }
                else
                {
                    pctCell.Value = item.Percentage;
                    pctCell.Style.NumberFormat.Format = "0.0%";
                }

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
