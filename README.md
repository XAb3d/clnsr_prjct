# Cleanser App (`clnsr_prjct`)

**XDS Credit Bureau — Data Cleanser Tool**

A C# / ASP.NET Blazor (.NET 9) application that validates, normalises, and prepares financial and credit records submitted by subscriber institutions before loading into the XDS credit bureau system.

---

## What It Does

- Accepts individual (IND) and commercial (BUS) subscriber data files
- Applies field-level cleaning rules (dates, phone numbers, ID numbers, account numbers, names, nationality codes, facility terms, etc.)
- Detects and routes duplicate records
- Matches submitted records against a reference database to identify new and updated records
- Outputs a multi-sheet Excel workbook: **CLEAN**, **UNLOADABLE**, and **DUPLICATE** sheets

---

## Project Structure

```
clnsr_prjct/
├── CleanserBlazorUI/
│   ├── Components/
│   │   └── Pages/
│   │       └── Home.razor          # Main page — orchestrates all cleaning & output
│   ├── Converters/
│   │   ├── IndividualDataTransformer.cs    # Cell-level cleaning — IND records
│   │   ├── IndividualDataTransformerDud.cs # Cleaning — IND unloadable path
│   │   ├── IndividualDataTransformerRef.cs # Cleaning — IND reference path
│   │   ├── BusinessDataTransformer.cs      # Cell-level cleaning — BUS records
│   │   ├── BusinessDataTransformerDud.cs   # Cleaning — BUS unloadable path
│   │   ├── BusinessDataTransformerRef.cs   # Cleaning — BUS reference path
│   │   └── ExcelProcessorService.cs        # Excel read/write helpers
│   ├── Data/
│   │   └── DataManagementService.cs        # SQL Server — reference DB access
│   ├── Entities/
│   │   ├── CellDataAndStatus.cs            # Core result object per field
│   │   ├── IndividualContext.cs            # Row-level context — IND
│   │   ├── BusinessContext.cs              # Row-level context — BUS
│   │   └── ...
│   ├── Helpers/
│   │   └── StringHelper.cs                 # Shared utility functions (~4800 lines)
│   ├── Repository/
│   │   └── DataCleaningType.cs
│   └── Constants/
└── tracker_rows.txt                        # Legacy issue notes (superseded by issues tracker)
```

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Blazor Server (.NET 9) |
| UI Components | MudBlazor |
| Excel I/O | ClosedXML / EPPlus |
| Database | SQL Server (EF Core + BulkExtensions) |
| Language | C# 13 |

---

## Branch & Commit Convention

| Branch | Purpose |
|---|---|
| `main` | Stable, tested baseline |
| `sprint/N-short-description` | Active sprint work |

Commit message format:
```
[#ISSUE_ID] Short description of change

- Detail 1
- Detail 2
```

Example:
```
[#7] Fix FacilityTerm round-up in IND and BUS transformers

- Changed (int)(value / 30.44) to (int)Math.Ceiling(value / 30.44)
- Applied to both IndividualDataTransformer and BusinessDataTransformer
```

---

## Issue Tracker

All known issues, severity ratings, priority levels, and resolution status are maintained in the formal issues tracker document (`Cleanser_Issues_Tracker_v2.xlsx`). See the accompanying Word report (`Cleanser_Issues_Report.docx`) for the full executive summary and issue descriptions.

### Current Sprint Plan

| Sprint | Issues | Focus |
|---|---|---|
| 1 | #3, #7, #9, #10, #11, #13, #15 | Close resolved issues + zero-risk fixes |
| 2 | #4, #5, #8, #9-BUS | Silent data deletion bugs |
| 3 | #12, #14, #16, #2 | Cross-record identity checks |
| 4 | #18 | Warning / Exception column |
| 5 | #1 | Reference file persistence |
| 6 | #19 | QA module integration |

---

## Getting Started

```bash
# Restore dependencies
dotnet restore

# Run locally
dotnet run --project CleanserBlazorUI
```

Requires a SQL Server connection string in `appsettings.json` for reference data persistence.

---

*Maintained by XDS Data Operations.*
