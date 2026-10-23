using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlanMPhase3GradingWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AllowedAttempts",
                table: "assignments",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "AllowedFileTypes",
                table: "assignments",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AnonymousGrading",
                table: "assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxFileSizeMb",
                table: "assignments",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<bool>(
                name: "ModeratedGrading",
                table: "assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ModeratorUserId",
                table: "assignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NamesRevealedAt",
                table: "assignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostingPolicy",
                table: "assignments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Automatic");

            migrationBuilder.AddColumn<int>(
                name: "CurrentAttemptNumber",
                table: "assignment_submissions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IsLate",
                table: "assignment_submissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostedAt",
                table: "assignment_submissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "submission_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    TextContent = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsLate = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submission_attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_submission_attempts_assignment_submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "assignment_submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "submission_comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: true),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorIsStaff = table.Column<bool>(type: "bit", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MediaStorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MediaContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MediaFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    IsPrivateToGraders = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submission_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_submission_comments_assignment_submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "assignment_submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "submission_provisional_grades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GraderUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Marks = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submission_provisional_grades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_submission_provisional_grades_assignment_submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "assignment_submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "submission_files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LegacyUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ConversionStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PdfStorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConversionError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ConversionAttempts = table.Column<int>(type: "int", nullable: false),
                    ConversionStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConvertedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submission_files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_submission_files_submission_attempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "submission_attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "submission_annotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GeometryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submission_annotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_submission_annotations_submission_files_SubmissionFileId",
                        column: x => x.SubmissionFileId,
                        principalTable: "submission_files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_submission_annotations_file_page",
                table: "submission_annotations",
                columns: new[] { "SubmissionFileId", "PageNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_submission_attempts_submission_number",
                table: "submission_attempts",
                columns: new[] { "SubmissionId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_submission_comments_submission",
                table: "submission_comments",
                columns: new[] { "SubmissionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_submission_files_attempt",
                table: "submission_files",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_submission_files_conversion",
                table: "submission_files",
                columns: new[] { "ConversionStatus", "ConversionStartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_submission_provisional_grades_grader",
                table: "submission_provisional_grades",
                columns: new[] { "SubmissionId", "GraderUserId" },
                unique: true);

            // Backfill: every existing submission becomes attempt #1; its file URL becomes a legacy file;
            // grades given before posting policies existed were visible at once, so they count as posted.
            migrationBuilder.Sql(@"
UPDATE s SET s.IsLate = CASE WHEN s.SubmittedAt > a.DueDate THEN 1 ELSE 0 END
FROM assignment_submissions s JOIN assignments a ON a.Id = s.AssignmentId;

UPDATE assignment_submissions SET PostedAt = COALESCE(GradedAt, SubmittedAt)
WHERE MarksAwarded IS NOT NULL AND Status = 'Graded' AND PostedAt IS NULL;

INSERT INTO submission_attempts (Id, SubmissionId, AttemptNumber, TextContent, SubmittedAt, IsLate, CreatedAt)
SELECT NEWID(), s.Id, 1, s.TextContent, s.SubmittedAt, s.IsLate, s.SubmittedAt
FROM assignment_submissions s
WHERE NOT EXISTS (SELECT 1 FROM submission_attempts x WHERE x.SubmissionId = s.Id);

INSERT INTO submission_files (Id, AttemptId, FileName, ContentType, SizeBytes, StorageKey, LegacyUrl, Sha256,
                              ConversionStatus, PdfStorageKey, ConversionError, ConversionAttempts, CreatedAt)
SELECT NEWID(), t.Id,
       LEFT(COALESCE(NULLIF(RIGHT(s.FileUrl, CHARINDEX('/', REVERSE(s.FileUrl) + '/') - 1), ''), 'submission'), 260),
       'application/octet-stream', 0, NULL, s.FileUrl, NULL, 'NotRequired', NULL, NULL, 0, s.SubmittedAt
FROM assignment_submissions s
JOIN submission_attempts t ON t.SubmissionId = s.Id AND t.AttemptNumber = 1
WHERE s.FileUrl IS NOT NULL AND LTRIM(RTRIM(s.FileUrl)) <> ''
  AND NOT EXISTS (SELECT 1 FROM submission_files f WHERE f.AttemptId = t.Id);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "submission_annotations");

            migrationBuilder.DropTable(
                name: "submission_comments");

            migrationBuilder.DropTable(
                name: "submission_provisional_grades");

            migrationBuilder.DropTable(
                name: "submission_files");

            migrationBuilder.DropTable(
                name: "submission_attempts");

            migrationBuilder.DropColumn(
                name: "AllowedAttempts",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "AllowedFileTypes",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "AnonymousGrading",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "MaxFileSizeMb",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "ModeratedGrading",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "ModeratorUserId",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "NamesRevealedAt",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "PostingPolicy",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "CurrentAttemptNumber",
                table: "assignment_submissions");

            migrationBuilder.DropColumn(
                name: "IsLate",
                table: "assignment_submissions");

            migrationBuilder.DropColumn(
                name: "PostedAt",
                table: "assignment_submissions");
        }
    }
}
