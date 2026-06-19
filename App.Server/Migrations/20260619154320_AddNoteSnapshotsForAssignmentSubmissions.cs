using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteSnapshotsForAssignmentSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NoteSnapshotId",
                table: "SubmittedWork",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NoteSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceNoteId = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceNoteGuid = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    TitleSnapshot = table.Column<string>(type: "TEXT", nullable: true),
                    ContentSnapshot = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SnapshotType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoteSnapshots_Notes_SourceNoteId",
                        column: x => x.SourceNoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NoteSnapshots_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedWork_NoteSnapshotId",
                table: "SubmittedWork",
                column: "NoteSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_NoteSnapshots_CreatedByUserId",
                table: "NoteSnapshots",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NoteSnapshots_SourceNoteId",
                table: "NoteSnapshots",
                column: "SourceNoteId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedWork_NoteSnapshots_NoteSnapshotId",
                table: "SubmittedWork",
                column: "NoteSnapshotId",
                principalTable: "NoteSnapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedWork_NoteSnapshots_NoteSnapshotId",
                table: "SubmittedWork");

            migrationBuilder.DropTable(
                name: "NoteSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedWork_NoteSnapshotId",
                table: "SubmittedWork");

            migrationBuilder.DropColumn(
                name: "NoteSnapshotId",
                table: "SubmittedWork");
        }
    }
}
