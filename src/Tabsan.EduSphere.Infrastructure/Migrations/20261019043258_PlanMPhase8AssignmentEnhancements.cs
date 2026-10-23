using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlanMPhase8AssignmentEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "discussion_threads",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableFrom",
                table: "assignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableUntil",
                table: "assignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GradeGroupIndividually",
                table: "assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupSetId",
                table: "assignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PeerReviewAnonymous",
                table: "assignments",
                type: "bit",
                nullable: false,
                // Peer reviews are anonymous unless the teacher turns it off.
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "PeerReviewCount",
                table: "assignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PeerReviewDueDate",
                table: "assignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "assignment_submissions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMissing",
                table: "assignment_submissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "LatePenalty",
                table: "assignment_submissions",
                type: "decimal(8,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "LatePenaltyWaived",
                table: "assignment_submissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RawMarks",
                table: "assignment_submissions",
                type: "decimal(8,2)",
                nullable: true);

            // Existing grades had no late deduction: the raw mark is the awarded mark.
            migrationBuilder.Sql("UPDATE assignment_submissions SET RawMarks = MarksAwarded WHERE MarksAwarded IS NOT NULL AND RawMarks IS NULL;");

            migrationBuilder.CreateTable(
                name: "assignment_overrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AvailableFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AvailableUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignment_overrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assignment_overrides_assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_sets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SelfSignup = table.Column<bool>(type: "bit", nullable: false),
                    MaxSize = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_sets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_group_sets_course_offerings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "course_offerings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "late_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LateDeductionEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DeductionPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Interval = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LowestGradePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MissingEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MissingGradePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_late_policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_late_policies_course_offerings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "course_offerings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "peer_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewerStudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RubricJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_peer_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_peer_reviews_assignment_submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "assignment_submissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_peer_reviews_assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "assignments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "student_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_groups_group_sets_GroupSetId",
                        column: x => x.GroupSetId,
                        principalTable: "group_sets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_group_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_group_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_group_members_student_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "student_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_discussion_threads_group",
                table: "discussion_threads",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_assignments_group_set",
                table: "assignments",
                column: "GroupSetId");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_submissions_group",
                table: "assignment_submissions",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_overrides_assignment",
                table: "assignment_overrides",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_group_sets_offering",
                table: "group_sets",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_late_policies_offering",
                table: "late_policies",
                column: "CourseOfferingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_peer_reviews_assignment_reviewer",
                table: "peer_reviews",
                columns: new[] { "AssignmentId", "ReviewerStudentProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_peer_reviews_submission_reviewer",
                table: "peer_reviews",
                columns: new[] { "SubmissionId", "ReviewerStudentProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_group_members_group",
                table: "student_group_members",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_student_group_members_set_student",
                table: "student_group_members",
                columns: new[] { "GroupSetId", "StudentProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_groups_set",
                table: "student_groups",
                column: "GroupSetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assignment_overrides");

            migrationBuilder.DropTable(
                name: "late_policies");

            migrationBuilder.DropTable(
                name: "peer_reviews");

            migrationBuilder.DropTable(
                name: "student_group_members");

            migrationBuilder.DropTable(
                name: "student_groups");

            migrationBuilder.DropTable(
                name: "group_sets");

            migrationBuilder.DropIndex(
                name: "IX_discussion_threads_group",
                table: "discussion_threads");

            migrationBuilder.DropIndex(
                name: "IX_assignments_group_set",
                table: "assignments");

            migrationBuilder.DropIndex(
                name: "IX_assignment_submissions_group",
                table: "assignment_submissions");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "discussion_threads");

            migrationBuilder.DropColumn(
                name: "AvailableFrom",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "AvailableUntil",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "GradeGroupIndividually",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "GroupSetId",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "PeerReviewAnonymous",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "PeerReviewCount",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "PeerReviewDueDate",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "assignment_submissions");

            migrationBuilder.DropColumn(
                name: "IsMissing",
                table: "assignment_submissions");

            migrationBuilder.DropColumn(
                name: "LatePenalty",
                table: "assignment_submissions");

            migrationBuilder.DropColumn(
                name: "LatePenaltyWaived",
                table: "assignment_submissions");

            migrationBuilder.DropColumn(
                name: "RawMarks",
                table: "assignment_submissions");
        }
    }
}
