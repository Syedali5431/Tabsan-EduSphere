using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CourseCopyAndTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "course_copy_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CopiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateShiftDays = table.Column<int>(type: "int", nullable: false),
                    KeptPublishedState = table.Column<bool>(type: "bit", nullable: false),
                    SummaryJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_copy_runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_course_copy_runs_course_offerings_TargetOfferingId",
                        column: x => x.TargetOfferingId,
                        principalTable: "course_offerings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "course_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Scope = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_course_templates_course_offerings_OfferingId",
                        column: x => x.OfferingId,
                        principalTable: "course_offerings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_course_copy_runs_target_created",
                table: "course_copy_runs",
                columns: new[] { "TargetOfferingId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_course_templates_offering",
                table: "course_templates",
                column: "OfferingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_copy_runs");

            migrationBuilder.DropTable(
                name: "course_templates");
        }
    }
}
