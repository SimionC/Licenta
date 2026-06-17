using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentsAndGrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "SubmittedWork",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "SubmittedWork",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "SubmittedWork",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoredFileName",
                table: "SubmittedWork",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudentId",
                table: "SubmittedWork",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "SubmittedWork",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "TextAnswer",
                table: "SubmittedWork",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SubmittedWork",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<decimal>(
                name: "GivenGrade",
                table: "Grades",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<DateTime>(
                name: "GradedAt",
                table: "Grades",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "WeightPercent",
                table: "CourseWork",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CourseWorkResources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CourseWorkId = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", nullable: false),
                    StoredFileName = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseWorkResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseWorkResources_CourseWork_CourseWorkId",
                        column: x => x.CourseWorkId,
                        principalTable: "CourseWork",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedWork_StudentId",
                table: "SubmittedWork",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseWorkResources_CourseWorkId",
                table: "CourseWorkResources",
                column: "CourseWorkId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedWork_Users_StudentId",
                table: "SubmittedWork",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedWork_Users_StudentId",
                table: "SubmittedWork");

            migrationBuilder.DropTable(
                name: "CourseWorkResources");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedWork_StudentId",
                table: "SubmittedWork");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "SubmittedWork");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "SubmittedWork");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "SubmittedWork");

            migrationBuilder.DropColumn(
                name: "StoredFileName",
                table: "SubmittedWork");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "SubmittedWork");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "SubmittedWork");

            migrationBuilder.DropColumn(
                name: "TextAnswer",
                table: "SubmittedWork");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SubmittedWork");

            migrationBuilder.DropColumn(
                name: "GradedAt",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "WeightPercent",
                table: "CourseWork");

            migrationBuilder.AlterColumn<int>(
                name: "GivenGrade",
                table: "Grades",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");
        }
    }
}
