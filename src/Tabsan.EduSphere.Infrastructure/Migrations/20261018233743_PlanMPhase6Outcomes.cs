using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlanMPhase6Outcomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MasteryPoints = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    CalculationMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CalculationParameter = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outcomes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outcome_alignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outcome_alignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_outcome_alignments_outcomes_OutcomeId",
                        column: x => x.OutcomeId,
                        principalTable: "outcomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "outcome_mappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outcome_mappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_outcome_mappings_outcomes_CourseOutcomeId",
                        column: x => x.CourseOutcomeId,
                        principalTable: "outcomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_outcome_mappings_outcomes_ProgramOutcomeId",
                        column: x => x.ProgramOutcomeId,
                        principalTable: "outcomes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "outcome_rating_levels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Points = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outcome_rating_levels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_outcome_rating_levels_outcomes_OutcomeId",
                        column: x => x.OutcomeId,
                        principalTable: "outcomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_outcome_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Percent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_outcome_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_outcome_results_outcome_alignments_AlignmentId",
                        column: x => x.AlignmentId,
                        principalTable: "outcome_alignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outcome_alignments_offering",
                table: "outcome_alignments",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_outcome_alignments_outcome_target",
                table: "outcome_alignments",
                columns: new[] { "OutcomeId", "TargetType", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outcome_alignments_target",
                table: "outcome_alignments",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_outcome_mappings_pair",
                table: "outcome_mappings",
                columns: new[] { "CourseOutcomeId", "ProgramOutcomeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outcome_mappings_program_outcome",
                table: "outcome_mappings",
                column: "ProgramOutcomeId");

            migrationBuilder.CreateIndex(
                name: "IX_outcome_rating_levels_OutcomeId",
                table: "outcome_rating_levels",
                column: "OutcomeId");

            migrationBuilder.CreateIndex(
                name: "IX_outcomes_course",
                table: "outcomes",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_outcomes_program",
                table: "outcomes",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_outcomes_scope_code",
                table: "outcomes",
                columns: new[] { "TenantId", "Level", "ProgramId", "CourseId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_student_outcome_results_AlignmentId",
                table: "student_outcome_results",
                column: "AlignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_student_outcome_results_offering_student",
                table: "student_outcome_results",
                columns: new[] { "CourseOfferingId", "StudentProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_student_outcome_results_outcome_student",
                table: "student_outcome_results",
                columns: new[] { "OutcomeId", "StudentProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_student_outcome_results_unique",
                table: "student_outcome_results",
                columns: new[] { "StudentProfileId", "AlignmentId", "SourceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outcome_mappings");

            migrationBuilder.DropTable(
                name: "outcome_rating_levels");

            migrationBuilder.DropTable(
                name: "student_outcome_results");

            migrationBuilder.DropTable(
                name: "outcome_alignments");

            migrationBuilder.DropTable(
                name: "outcomes");
        }
    }
}
