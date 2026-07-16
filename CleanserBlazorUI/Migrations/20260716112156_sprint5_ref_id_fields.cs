using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanserBlazorUI.Migrations
{
    /// <inheritdoc />
    public partial class sprint5_ref_id_fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DriverLicNum",
                table: "IndividualsData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EzwichNum",
                table: "IndividualsData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedDate",
                table: "IndividualsData",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "NatIDNum",
                table: "IndividualsData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherIDNum",
                table: "IndividualsData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PassportNum",
                table: "IndividualsData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SSNum",
                table: "IndividualsData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VotersIDNum",
                table: "IndividualsData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedDate",
                table: "BusinessesData",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverLicNum",
                table: "IndividualsData");

            migrationBuilder.DropColumn(
                name: "EzwichNum",
                table: "IndividualsData");

            migrationBuilder.DropColumn(
                name: "LastUpdatedDate",
                table: "IndividualsData");

            migrationBuilder.DropColumn(
                name: "NatIDNum",
                table: "IndividualsData");

            migrationBuilder.DropColumn(
                name: "OtherIDNum",
                table: "IndividualsData");

            migrationBuilder.DropColumn(
                name: "PassportNum",
                table: "IndividualsData");

            migrationBuilder.DropColumn(
                name: "SSNum",
                table: "IndividualsData");

            migrationBuilder.DropColumn(
                name: "VotersIDNum",
                table: "IndividualsData");

            migrationBuilder.DropColumn(
                name: "LastUpdatedDate",
                table: "BusinessesData");
        }
    }
}
