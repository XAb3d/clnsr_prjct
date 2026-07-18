using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanserBlazorUI.Migrations
{
    /// <summary>
    /// Sprint 10 migration — adds Surname and FirstName to IndividualsData so the
    /// reference matcher can compare names across months and bypass business-keyword
    /// checks for names that were manually reviewed and accepted in the previous clean.
    /// </summary>
    public partial class sprint10_ref_name_fields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Surname",
                table: "IndividualsData",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "IndividualsData",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn("Surname",   "IndividualsData");
            migrationBuilder.DropColumn("FirstName", "IndividualsData");
        }
    }
}
