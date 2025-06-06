using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddNotesAndCollaborationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ----- COMMENTED OUT: Legacy/old Note table modifications -----
            /*
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Notes_Notes_NoteId",
                table: "Courses_Notes");

            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Users_UserId",
                table: "Notes");

            migrationBuilder.DropForeignKey(
                name: "FK_Notes_VisibilityType_VisibilityTypeId",
                table: "Notes");

            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedWork_Notes_NoteId",
                table: "SubmittedWork");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Notes_Notes_NoteId",
                table: "Users_Notes");

            migrationBuilder.DropIndex(
                name: "IX_Notes_Guid",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Notes_UserId",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Notes_VisibilityTypeId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "CreationDate",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "ModifyDate",
                table: "Notes");

            migrationBuilder.RenameColumn(
                name: "VisibilityTypeId",
                table: "Notes",
                newName: "IsPublic");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Notes",
                newName: "IsLatex");

            migrationBuilder.RenameColumn(
                name: "Text",
                table: "Notes",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "Guid",
                table: "Notes",
                newName: "Title");

            migrationBuilder.AddColumn<int>(
                name: "CollaborationId",
                table: "Notes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Notes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Notes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsCode",
                table: "Notes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Notes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
            */
            // ----- END COMMENTED OUT SECTION -----

            // ---- KEEP: ONLY THE NEW TABLES ----
            migrationBuilder.CreateTable(
                name: "Collaborations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collaborations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NoteId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CanEdit = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanRead = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotePermissions_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollaborationMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CollaborationId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollaborationMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollaborationMembers_Collaborations_CollaborationId",
                        column: x => x.CollaborationId,
                        principalTable: "Collaborations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationMembers_CollaborationId",
                table: "CollaborationMembers",
                column: "CollaborationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotePermissions_NoteId",
                table: "NotePermissions",
                column: "NoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ----- COMMENTED OUT: Legacy/old Note table modifications -----
            /*
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Notes_Note_NoteId",
                table: "Courses_Notes");

            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Collaborations_CollaborationId",
                table: "Notes");

            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedWork_Note_NoteId",
                table: "SubmittedWork");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Notes_Note_NoteId",
                table: "Users_Notes");
            */
            // ----- END COMMENTED OUT SECTION -----

            migrationBuilder.DropTable(
                name: "CollaborationMembers");

            migrationBuilder.DropTable(
                name: "NotePermissions");

            migrationBuilder.DropTable(
                name: "Collaborations");

            // ----- COMMENTED OUT: Legacy/old Note table modifications -----
            /*
            migrationBuilder.DropIndex(
                name: "IX_Notes_CollaborationId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "CollaborationId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "IsCode",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Notes");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Notes",
                newName: "Text");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Notes",
                newName: "Guid");

            migrationBuilder.RenameColumn(
                name: "IsPublic",
                table: "Notes",
                newName: "VisibilityTypeId");

            migrationBuilder.RenameColumn(
                name: "IsLatex",
                table: "Notes",
                newName: "UserId");

            migrationBuilder.AddColumn<string>(
                name: "CreationDate",
                table: "Notes",
                type: "TEXT",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "ModifyDate",
                table: "Notes",
                type: "TEXT",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_Guid",
                table: "Notes",
                column: "Guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notes_UserId",
                table: "Notes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_VisibilityTypeId",
                table: "Notes",
                column: "VisibilityTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Notes_Notes_NoteId",
                table: "Courses_Notes",
                column: "NoteId",
                principalTable: "Notes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Users_UserId",
                table: "Notes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_VisibilityType_VisibilityTypeId",
                table: "Notes",
                column: "VisibilityTypeId",
                principalTable: "VisibilityType",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedWork_Notes_NoteId",
                table: "SubmittedWork",
                column: "NoteId",
                principalTable: "Notes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Notes_Notes_NoteId",
                table: "Users_Notes",
                column: "NoteId",
                principalTable: "Notes",
                principalColumn: "Id");
            */
            // ----- END COMMENTED OUT SECTION -----
        }
    }
}
