using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlanDPhase5Stage52AnalyticsIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "assignment_submissions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_results_offering_published",
                table: "results",
                columns: new[] { "CourseOfferingId", "IsPublished" });

            migrationBuilder.CreateIndex(
                name: "IX_results_published_published_at",
                table: "results",
                columns: new[] { "IsPublished", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_quiz_attempts_quiz_status",
                table: "quiz_attempts",
                columns: new[] { "QuizId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_assignment_submissions_assignment_status",
                table: "assignment_submissions",
                columns: new[] { "AssignmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_assignment_submissions_student_id",
                table: "assignment_submissions",
                column: "StudentProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_results_offering_published",
                table: "results");

            migrationBuilder.DropIndex(
                name: "IX_results_published_published_at",
                table: "results");

            migrationBuilder.DropIndex(
                name: "IX_quiz_attempts_quiz_status",
                table: "quiz_attempts");

            migrationBuilder.DropIndex(
                name: "IX_assignment_submissions_assignment_status",
                table: "assignment_submissions");

            migrationBuilder.DropIndex(
                name: "IX_assignment_submissions_student_id",
                table: "assignment_submissions");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "assignment_submissions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);
        }
    }
}
