using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlanMPhase2QuizEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessCode",
                table: "quizzes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowedIpRanges",
                table: "quizzes",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LockAnsweredQuestions",
                table: "quizzes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OneQuestionAtATime",
                table: "quizzes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReviewAfterCloseOnly",
                table: "quizzes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReviewVisibility",
                table: "quizzes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ScoreOnly"); // existing quizzes: students see their score only

            migrationBuilder.AddColumn<bool>(
                name: "ShuffleOptions",
                table: "quizzes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShuffleQuestions",
                table: "quizzes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "QuizId",
                table: "quiz_questions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "ConfigJson",
                table: "quiz_questions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                table: "quiz_questions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "quiz_questions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "QuestionBankId",
                table: "quiz_questions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "quiz_questions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeadlineAt",
                table: "quiz_attempts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartedFromIp",
                table: "quiz_attempts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TextResponse",
                table: "quiz_answers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "quiz_answers",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GradedAt",
                table: "quiz_answers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GradedByUserId",
                table: "quiz_answers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseJson",
                table: "quiz_answers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SavedAt",
                table: "quiz_answers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "question_banks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsShared = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_banks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "quiz_accommodations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuizId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtraTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    TimeMultiplier = table.Column<decimal>(type: "decimal(4,2)", nullable: false, defaultValue: 1m),
                    ExtraAttempts = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quiz_accommodations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "quiz_answer_selections",
                columns: table => new
                {
                    QuizAnswerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuizOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quiz_answer_selections", x => new { x.QuizAnswerId, x.QuizOptionId });
                    table.ForeignKey(
                        name: "FK_quiz_answer_selections_quiz_answers_QuizAnswerId",
                        column: x => x.QuizAnswerId,
                        principalTable: "quiz_answers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quiz_answer_selections_quiz_options_QuizOptionId",
                        column: x => x.QuizOptionId,
                        principalTable: "quiz_options",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quiz_attempt_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuizAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuizQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Marks = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    QuizQuestionGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PresentationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedValuesJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quiz_attempt_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quiz_attempt_items_quiz_attempts_QuizAttemptId",
                        column: x => x.QuizAttemptId,
                        principalTable: "quiz_attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quiz_attempt_items_quiz_questions_QuizQuestionId",
                        column: x => x.QuizQuestionId,
                        principalTable: "quiz_questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quiz_availability_overrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuizId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AvailableFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AvailableUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quiz_availability_overrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quiz_availability_overrides_quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quiz_question_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuizId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionBankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PickCount = table.Column<int>(type: "int", nullable: false),
                    MarksPerQuestion = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    TagFilter = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quiz_question_groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quiz_question_groups_question_banks_QuestionBankId",
                        column: x => x.QuestionBankId,
                        principalTable: "question_banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quiz_question_groups_quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quiz_questions_QuestionBankId_IsArchived",
                table: "quiz_questions",
                columns: new[] { "QuestionBankId", "IsArchived" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_quiz_questions_owner",
                table: "quiz_questions",
                sql: "([QuizId] IS NOT NULL AND [QuestionBankId] IS NULL) OR ([QuizId] IS NULL AND [QuestionBankId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_question_banks_TenantId_CourseOfferingId",
                table: "question_banks",
                columns: new[] { "TenantId", "CourseOfferingId" });

            migrationBuilder.CreateIndex(
                name: "IX_question_banks_TenantId_DepartmentId",
                table: "question_banks",
                columns: new[] { "TenantId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_quiz_accommodations_CourseOfferingId_QuizId_StudentProfileId",
                table: "quiz_accommodations",
                columns: new[] { "CourseOfferingId", "QuizId", "StudentProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quiz_accommodations_StudentProfileId",
                table: "quiz_accommodations",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_quiz_answer_selections_QuizOptionId",
                table: "quiz_answer_selections",
                column: "QuizOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_quiz_attempt_items_QuizAttemptId_QuizQuestionId",
                table: "quiz_attempt_items",
                columns: new[] { "QuizAttemptId", "QuizQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quiz_attempt_items_QuizQuestionId",
                table: "quiz_attempt_items",
                column: "QuizQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_quiz_availability_overrides_QuizId_StudentProfileId",
                table: "quiz_availability_overrides",
                columns: new[] { "QuizId", "StudentProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quiz_question_groups_QuestionBankId",
                table: "quiz_question_groups",
                column: "QuestionBankId");

            migrationBuilder.CreateIndex(
                name: "IX_quiz_question_groups_QuizId",
                table: "quiz_question_groups",
                column: "QuizId");

            migrationBuilder.AddForeignKey(
                name: "FK_quiz_questions_question_banks_QuestionBankId",
                table: "quiz_questions",
                column: "QuestionBankId",
                principalTable: "question_banks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            // Plan M2.1 backfill: every existing single selection becomes a selection row,
            // so graders and statistics read one source of truth (SelectedOptionId is kept for compatibility).
            migrationBuilder.Sql(@"
INSERT INTO [quiz_answer_selections] ([QuizAnswerId], [QuizOptionId])
SELECT a.[Id], a.[SelectedOptionId]
FROM [quiz_answers] a
INNER JOIN [quiz_options] o ON o.[Id] = a.[SelectedOptionId]
WHERE a.[SelectedOptionId] IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [quiz_answer_selections] s WHERE s.[QuizAnswerId] = a.[Id] AND s.[QuizOptionId] = a.[SelectedOptionId]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_quiz_questions_question_banks_QuestionBankId",
                table: "quiz_questions");

            migrationBuilder.DropTable(
                name: "quiz_accommodations");

            migrationBuilder.DropTable(
                name: "quiz_answer_selections");

            migrationBuilder.DropTable(
                name: "quiz_attempt_items");

            migrationBuilder.DropTable(
                name: "quiz_availability_overrides");

            migrationBuilder.DropTable(
                name: "quiz_question_groups");

            migrationBuilder.DropTable(
                name: "question_banks");

            migrationBuilder.DropIndex(
                name: "IX_quiz_questions_QuestionBankId_IsArchived",
                table: "quiz_questions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_quiz_questions_owner",
                table: "quiz_questions");

            migrationBuilder.DropColumn(
                name: "AccessCode",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "AllowedIpRanges",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "LockAnsweredQuestions",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "OneQuestionAtATime",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "ReviewAfterCloseOnly",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "ReviewVisibility",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "ShuffleOptions",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "ShuffleQuestions",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "ConfigJson",
                table: "quiz_questions");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "quiz_questions");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "quiz_questions");

            migrationBuilder.DropColumn(
                name: "QuestionBankId",
                table: "quiz_questions");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "quiz_questions");

            migrationBuilder.DropColumn(
                name: "DeadlineAt",
                table: "quiz_attempts");

            migrationBuilder.DropColumn(
                name: "StartedFromIp",
                table: "quiz_attempts");

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "quiz_answers");

            migrationBuilder.DropColumn(
                name: "GradedAt",
                table: "quiz_answers");

            migrationBuilder.DropColumn(
                name: "GradedByUserId",
                table: "quiz_answers");

            migrationBuilder.DropColumn(
                name: "ResponseJson",
                table: "quiz_answers");

            migrationBuilder.DropColumn(
                name: "SavedAt",
                table: "quiz_answers");

            migrationBuilder.AlterColumn<Guid>(
                name: "QuizId",
                table: "quiz_questions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TextResponse",
                table: "quiz_answers",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
