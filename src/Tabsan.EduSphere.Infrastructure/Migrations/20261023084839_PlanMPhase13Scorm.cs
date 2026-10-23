using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlanMPhase13Scorm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ScormPackageId",
                table: "assignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "scorm_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    CompletionStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SuccessStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScoreRaw = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    ScoreMin = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    ScoreMax = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    ScoreScaled = table.Column<decimal>(type: "decimal(7,6)", precision: 7, scale: 6, nullable: true),
                    ScorePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SuspendData = table.Column<string>(type: "nvarchar(max)", maxLength: 64000, nullable: true),
                    Exit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CmiJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalSeconds = table.Column<int>(type: "int", nullable: false),
                    CommitCount = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastCommitAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scorm_attempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "scorm_packages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Identifier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LaunchPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileCount = table.Column<int>(type: "int", nullable: false),
                    ScoCount = table.Column<int>(type: "int", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    MasteryScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaxMarks = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsRemoved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scorm_packages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "xapi_credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SecretHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_xapi_credentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "xapi_statements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CredentialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorKey = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    VerbId = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ObjectId = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Registration = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScoreScaled = table.Column<decimal>(type: "decimal(7,6)", precision: 7, scale: 6, nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: true),
                    Completion = table.Column<bool>(type: "bit", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Stored = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsVoided = table.Column<bool>(type: "bit", nullable: false),
                    Json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_xapi_statements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_scorm_attempts_package_student_number",
                table: "scorm_attempts",
                columns: new[] { "PackageId", "StudentProfileId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scorm_attempts_student",
                table: "scorm_attempts",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_scorm_packages_offering",
                table: "scorm_packages",
                columns: new[] { "OfferingId", "IsRemoved" });

            migrationBuilder.CreateIndex(
                name: "IX_xapi_credentials_key",
                table: "xapi_credentials",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_xapi_statements_actor_stored",
                table: "xapi_statements",
                columns: new[] { "ActorKey", "Stored" });

            migrationBuilder.CreateIndex(
                name: "IX_xapi_statements_statement_id",
                table: "xapi_statements",
                column: "StatementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_xapi_statements_stored",
                table: "xapi_statements",
                column: "Stored");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scorm_attempts");

            migrationBuilder.DropTable(
                name: "scorm_packages");

            migrationBuilder.DropTable(
                name: "xapi_credentials");

            migrationBuilder.DropTable(
                name: "xapi_statements");

            migrationBuilder.DropColumn(
                name: "ScormPackageId",
                table: "assignments");
        }
    }
}
