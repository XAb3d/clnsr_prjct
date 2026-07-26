using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanserBlazorUI.Migrations
{
    /// <inheritdoc />
    public partial class sprint10_ref_name_fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Surname and FirstName columns already exist in IndividualsData
            // (added directly to the DB prior to this migration being generated).
            // No action needed — migration marked as applied to keep EF in sync.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Columns retained on rollback — removing them could cause data loss.
        }
    }
}
