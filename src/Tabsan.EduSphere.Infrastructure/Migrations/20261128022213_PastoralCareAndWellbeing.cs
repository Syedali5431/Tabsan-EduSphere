using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PastoralCareAndWellbeing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pastoral_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    Tone = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pastoral_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pastoral_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CampusId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    Tone = table.Column<byte>(type: "tinyint", nullable: false),
                    Severity = table.Column<byte>(type: "tinyint", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ActionTaken = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Visibility = table.Column<byte>(type: "tinyint", nullable: false),
                    SharedWithStudent = table.Column<bool>(type: "bit", nullable: false),
                    SharedWithParents = table.Column<bool>(type: "bit", nullable: false),
                    SharedNotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FollowUp = table.Column<byte>(type: "tinyint", nullable: false),
                    FollowUpUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FollowUpBy = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FollowUpRemindedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FollowUpDoneAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FollowUpDoneByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RemovedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pastoral_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pastoral_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckInsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ConcernAlertCount = table.Column<int>(type: "int", nullable: false),
                    ConcernAlertDays = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pastoral_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pastoral_team_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampusId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SeesConfidential = table.Column<bool>(type: "bit", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pastoral_team_members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "support_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CampusId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Needs = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Strategies = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    StartsOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewRemindedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Visibility = table.Column<byte>(type: "tinyint", nullable: false),
                    SharedWithStudent = table.Column<bool>(type: "bit", nullable: false),
                    SharedWithParents = table.Column<bool>(type: "bit", nullable: false),
                    SharedNotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wellbeing_check_ins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CampusId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mood = table.Column<byte>(type: "tinyint", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WantsToTalk = table.Column<bool>(type: "bit", nullable: false),
                    CheckedInAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcknowledgeNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AlertedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wellbeing_check_ins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pastoral_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pastoral_notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pastoral_notes_pastoral_records_RecordId",
                        column: x => x.RecordId,
                        principalTable: "pastoral_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pastoral_categories_tenant",
                table: "pastoral_categories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_pastoral_notes_record",
                table: "pastoral_notes",
                column: "RecordId");

            migrationBuilder.CreateIndex(
                name: "IX_pastoral_records_follow_up",
                table: "pastoral_records",
                columns: new[] { "FollowUp", "FollowUpUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_pastoral_records_student",
                table: "pastoral_records",
                columns: new[] { "StudentProfileId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_pastoral_records_tenant",
                table: "pastoral_records",
                columns: new[] { "TenantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "UX_pastoral_settings_tenant",
                table: "pastoral_settings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pastoral_team_members_tenant",
                table: "pastoral_team_members",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_pastoral_team_members_user",
                table: "pastoral_team_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_support_plans_student",
                table: "support_plans",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_support_plans_tenant",
                table: "support_plans",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_wellbeing_check_ins_student",
                table: "wellbeing_check_ins",
                columns: new[] { "StudentProfileId", "CheckedInAt" });

            migrationBuilder.CreateIndex(
                name: "IX_wellbeing_check_ins_tenant",
                table: "wellbeing_check_ins",
                columns: new[] { "TenantId", "CheckedInAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pastoral_categories");

            migrationBuilder.DropTable(
                name: "pastoral_notes");

            migrationBuilder.DropTable(
                name: "pastoral_settings");

            migrationBuilder.DropTable(
                name: "pastoral_team_members");

            migrationBuilder.DropTable(
                name: "support_plans");

            migrationBuilder.DropTable(
                name: "wellbeing_check_ins");

            migrationBuilder.DropTable(
                name: "pastoral_records");
        }
    }
}
