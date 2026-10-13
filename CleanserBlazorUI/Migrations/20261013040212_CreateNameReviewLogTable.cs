using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanserBlazorUI.Migrations
{
    /// <inheritdoc />
    public partial class CreateNameReviewLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NameReviewLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileKind = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    SubscriberCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FlagTypes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RawSurname = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RawFirstName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RawMiddleNames = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RawBusinessName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    CorrectedSurname = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CorrectedFirstName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CorrectedMiddleNames = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CorrectedBusinessName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    AnchorDob = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    AnchorIds = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DecidedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NameReviewLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NameReviewLog_Lookup",
                table: "NameReviewLog",
                columns: new[] { "SubscriberCode", "FileKind", "CustomerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NameReviewLog");
        }
    }
}
