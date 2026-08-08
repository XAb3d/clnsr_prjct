using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanserBlazorUI.Migrations
{
    /// <inheritdoc />
    public partial class AddUnloadableLogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SubscriberCode",
                table: "SubscriberProfiles",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "UnloadableLogHeaders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriberProfileId = table.Column<int>(type: "int", nullable: false),
                    Associate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Filename = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumberOfRecords = table.Column<int>(type: "int", nullable: false),
                    ReportingPeriod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportingYear = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Months = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogYear = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateEmailed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateFixed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnloadableLogHeaders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnloadableLogHeaders_SubscriberProfiles_SubscriberProfileId",
                        column: x => x.SubscriberProfileId,
                        principalTable: "SubscriberProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnloadableLogCategoryDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnloadableLogHeaderId = table.Column<int>(type: "int", nullable: false),
                    TopLevelCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DescriptionOfErrors = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VolumeAffected = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnloadableLogCategoryDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnloadableLogCategoryDetails_UnloadableLogHeaders_UnloadableLogHeaderId",
                        column: x => x.UnloadableLogHeaderId,
                        principalTable: "UnloadableLogHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnloadableLogMessageDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnloadableLogHeaderId = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<double>(type: "float", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnloadableLogMessageDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnloadableLogMessageDetails_UnloadableLogHeaders_UnloadableLogHeaderId",
                        column: x => x.UnloadableLogHeaderId,
                        principalTable: "UnloadableLogHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriberProfiles_SubscriberCode",
                table: "SubscriberProfiles",
                column: "SubscriberCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnloadableLogCategoryDetails_UnloadableLogHeaderId",
                table: "UnloadableLogCategoryDetails",
                column: "UnloadableLogHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_UnloadableLogHeaders_SubscriberProfileId",
                table: "UnloadableLogHeaders",
                column: "SubscriberProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UnloadableLogMessageDetails_UnloadableLogHeaderId",
                table: "UnloadableLogMessageDetails",
                column: "UnloadableLogHeaderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnloadableLogCategoryDetails");

            migrationBuilder.DropTable(
                name: "UnloadableLogMessageDetails");

            migrationBuilder.DropTable(
                name: "UnloadableLogHeaders");

            migrationBuilder.DropIndex(
                name: "IX_SubscriberProfiles_SubscriberCode",
                table: "SubscriberProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "SubscriberCode",
                table: "SubscriberProfiles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
