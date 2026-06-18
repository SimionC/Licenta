using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Server.Migrations
{
    /// <inheritdoc />
    public partial class DisablePublicNoteLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Notes SET VisibilityTypeId = 1 WHERE VisibilityTypeId = 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Public-link note sharing was intentionally removed.
        }
    }
}
