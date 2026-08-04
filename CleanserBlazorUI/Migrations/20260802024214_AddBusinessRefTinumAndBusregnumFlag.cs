using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanserBlazorUI.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessRefTinumAndBusregnumFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tinum",
                table: "BusinessesData",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tinum",
                table: "BusinessesData");
        }
    }
}
