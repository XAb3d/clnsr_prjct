using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanserBlazorUI.Migrations
{
    /// <inheritdoc />
    public partial class AddFacilityStatusAndBusinessIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacilityStatusCode",
                table: "IndividualsData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Businessname",
                table: "BusinessesData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Busregnum",
                table: "BusinessesData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacilityStatusCode",
                table: "BusinessesData",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FacilityStatusCode",
                table: "IndividualsData");

            migrationBuilder.DropColumn(
                name: "Businessname",
                table: "BusinessesData");

            migrationBuilder.DropColumn(
                name: "Busregnum",
                table: "BusinessesData");

            migrationBuilder.DropColumn(
                name: "FacilityStatusCode",
                table: "BusinessesData");
        }
    }
}
