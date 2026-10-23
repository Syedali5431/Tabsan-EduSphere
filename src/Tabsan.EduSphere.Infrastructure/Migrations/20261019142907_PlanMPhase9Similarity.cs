using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlanMPhase9Similarity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SimilarityCheckEnabled",
                table: "assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SimilarityVisibleToStudents",
                table: "assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "similarity_checks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    SourcesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WebhookTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ReportUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Tries = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_similarity_checks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_similarity_checks_assignment_submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "assignment_submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "similarity_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WordCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_similarity_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_similarity_documents_assignment_submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "assignment_submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "similarity_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccountEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ApiKeyEncrypted = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Sandbox = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_similarity_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "similarity_fingerprints",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Hash = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_similarity_fingerprints", x => new { x.DocumentId, x.Position });
                    table.ForeignKey(
                        name: "FK_similarity_fingerprints_similarity_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "similarity_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_similarity_checks_assignment",
                table: "similarity_checks",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_similarity_checks_queue",
                table: "similarity_checks",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_similarity_checks_submission",
                table: "similarity_checks",
                columns: new[] { "SubmissionId", "AttemptNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_similarity_documents_assignment",
                table: "similarity_documents",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_similarity_documents_offering",
                table: "similarity_documents",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_similarity_documents_submission_attempt",
                table: "similarity_documents",
                columns: new[] { "SubmissionId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_similarity_documents_tenant",
                table: "similarity_documents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_similarity_fingerprints_hash",
                table: "similarity_fingerprints",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_similarity_settings_tenant",
                table: "similarity_settings",
                column: "TenantId",
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "similarity_checks");

            migrationBuilder.DropTable(
                name: "similarity_fingerprints");

            migrationBuilder.DropTable(
                name: "similarity_settings");

            migrationBuilder.DropTable(
                name: "similarity_documents");

            migrationBuilder.DropColumn(
                name: "SimilarityCheckEnabled",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "SimilarityVisibleToStudents",
                table: "assignments");
        }
    }
}
