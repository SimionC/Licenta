using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectNoteSharingPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotePermissions_NoteId",
                table: "NotePermissions");

            migrationBuilder.AddColumn<int>(
                name: "InvitedByUserId",
                table: "NotePermissions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "NotePermissions",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "NotePermissions",
                type: "TEXT",
                nullable: false,
                defaultValue: "viewer");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "NotePermissions",
                type: "TEXT",
                nullable: false,
                defaultValue: "accepted");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "NotePermissions",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.Sql("""
                UPDATE NotePermissions
                SET Role = CASE WHEN CanEdit = 1 THEN 'editor' ELSE 'viewer' END,
                    Status = 'accepted',
                    InvitedByUserId = (
                        SELECT Notes.UserId
                        FROM Notes
                        WHERE Notes.Id = NotePermissions.NoteId
                    ),
                    CreatedAt = CURRENT_TIMESTAMP,
                    UpdatedAt = CURRENT_TIMESTAMP
                """);

            migrationBuilder.DropColumn(
                name: "CanEdit",
                table: "NotePermissions");

            migrationBuilder.DropColumn(
                name: "CanRead",
                table: "NotePermissions");

            migrationBuilder.CreateIndex(
                name: "IX_NotePermissions_InvitedByUserId",
                table: "NotePermissions",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotePermissions_NoteId_UserId",
                table: "NotePermissions",
                columns: new[] { "NoteId", "UserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NotePermissions_Users_InvitedByUserId",
                table: "NotePermissions",
                column: "InvitedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotePermissions_Users_InvitedByUserId",
                table: "NotePermissions");

            migrationBuilder.DropIndex(
                name: "IX_NotePermissions_InvitedByUserId",
                table: "NotePermissions");

            migrationBuilder.DropIndex(
                name: "IX_NotePermissions_NoteId_UserId",
                table: "NotePermissions");

            migrationBuilder.AddColumn<bool>(
                name: "CanRead",
                table: "NotePermissions",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanEdit",
                table: "NotePermissions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE NotePermissions
                SET CanEdit = CASE WHEN Role = 'editor' THEN 1 ELSE 0 END,
                    CanRead = 1
                """);

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "NotePermissions");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "NotePermissions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "NotePermissions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "NotePermissions");

            migrationBuilder.DropColumn(
                name: "InvitedByUserId",
                table: "NotePermissions");

            migrationBuilder.CreateIndex(
                name: "IX_NotePermissions_NoteId",
                table: "NotePermissions",
                column: "NoteId");
        }
    }
}
