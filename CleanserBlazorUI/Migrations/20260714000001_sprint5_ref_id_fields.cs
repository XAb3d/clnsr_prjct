using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanserBlazorUI.Migrations
{
    /// <summary>
    /// Sprint 5 migration — adds personal ID fields to IndividualsData for cross-month
    /// enrichment tracking, and adds LastUpdatedDate to both IndividualsData and BusinessesData.
    ///
    /// All new columns are nullable — no data loss on existing rows.
    /// SQL Server handles ADD COLUMN NULL as a metadata-only operation (instant, no row rewrite).
    /// </summary>
    public partial class sprint5_ref_id_fields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── IndividualsData: 7 personal ID fields ─────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "NatIDNum",
                table: "IndividualsData",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VotersIDNum",
                table: "IndividualsData",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DriverLicNum",
                table: "IndividualsData",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PassportNum",
                table: "IndividualsData",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SSNum",
                table: "IndividualsData",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EzwichNum",
                table: "IndividualsData",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OtherIDNum",
                table: "IndividualsData",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "");

            // ── IndividualsData: LastUpdatedDate ──────────────────────────────
            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedDate",
                table: "IndividualsData",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            // ── BusinessesData: LastUpdatedDate ───────────────────────────────
            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedDate",
                table: "BusinessesData",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            // ── Composite index on IndividualsData for upsert performance ─────
            // Upsert key: (SubscriberCode, CreditFacilityAccNum, CustomerID, DisbursementDate)
            // Without this index, the upsert lookup scans the full table per record.
            // nvarchar(max) columns can't be indexed directly — index on computed hash approach
            // is not needed here since EF lookups will use the in-memory dictionary;
            // this index covers the WHERE clause in GETReferenceData_IND bulk load.
            migrationBuilder.CreateIndex(
                name: "IX_IndividualsData_SubscriberCode",
                table: "IndividualsData",
                column: "SubscriberCode");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessesData_SubscriberCode",
                table: "BusinessesData",
                column: "SubscriberCode");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex("IX_IndividualsData_SubscriberCode", "IndividualsData");
            migrationBuilder.DropIndex("IX_BusinessesData_SubscriberCode",  "BusinessesData");

            migrationBuilder.DropColumn("NatIDNum",     "IndividualsData");
            migrationBuilder.DropColumn("VotersIDNum",  "IndividualsData");
            migrationBuilder.DropColumn("DriverLicNum", "IndividualsData");
            migrationBuilder.DropColumn("PassportNum",  "IndividualsData");
            migrationBuilder.DropColumn("SSNum",        "IndividualsData");
            migrationBuilder.DropColumn("EzwichNum",    "IndividualsData");
            migrationBuilder.DropColumn("OtherIDNum",   "IndividualsData");
            migrationBuilder.DropColumn("LastUpdatedDate", "IndividualsData");
            migrationBuilder.DropColumn("LastUpdatedDate", "BusinessesData");
        }
    }
}
