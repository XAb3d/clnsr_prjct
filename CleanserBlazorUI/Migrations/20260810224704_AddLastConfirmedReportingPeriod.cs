using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanserBlazorUI.Migrations
{
    /// <inheritdoc />
    public partial class AddLastConfirmedReportingPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastConfirmedReportingPeriod",
                table: "IndividualsData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastConfirmedReportingPeriod",
                table: "BusinessesData",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastConfirmedReportingPeriod",
                table: "IndividualsData");

            migrationBuilder.DropColumn(
                name: "LastConfirmedReportingPeriod",
                table: "BusinessesData");
        }
    }
}
