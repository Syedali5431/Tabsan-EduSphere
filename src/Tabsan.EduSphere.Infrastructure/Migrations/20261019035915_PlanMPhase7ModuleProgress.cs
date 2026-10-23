using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlanMPhase7ModuleProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletionMode",
                table: "course_content_modules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequireSequentialProgress",
                table: "course_content_modules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnlockAt",
                table: "course_content_modules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "module_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 50000, nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false),
                    IndentLevel = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    RequirementType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MinScorePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_module_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_module_items_course_content_modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "course_content_modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "module_prerequisites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrerequisiteModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_module_prerequisites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_module_prerequisites_course_content_modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "course_content_modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_module_prerequisites_course_content_modules_PrerequisiteModuleId",
                        column: x => x.PrerequisiteModuleId,
                        principalTable: "course_content_modules",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "module_item_progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MarkedDoneAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_module_item_progress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_module_item_progress_module_items_ModuleItemId",
                        column: x => x.ModuleItemId,
                        principalTable: "module_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_module_item_progress_ModuleItemId",
                table: "module_item_progress",
                column: "ModuleItemId");

            migrationBuilder.CreateIndex(
                name: "IX_module_item_progress_offering_student",
                table: "module_item_progress",
                columns: new[] { "OfferingId", "StudentProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_module_item_progress_student_item",
                table: "module_item_progress",
                columns: new[] { "StudentProfileId", "ModuleItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_module_items_module_position",
                table: "module_items",
                columns: new[] { "ModuleId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_module_items_offering",
                table: "module_items",
                column: "OfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_module_items_target",
                table: "module_items",
                columns: new[] { "ItemType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_module_prerequisites_pair",
                table: "module_prerequisites",
                columns: new[] { "ModuleId", "PrerequisiteModuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_module_prerequisites_PrerequisiteModuleId",
                table: "module_prerequisites",
                column: "PrerequisiteModuleId");

            // Plan M7: existing module videos become the modules' first items (in the order they were added).
            // The module body stays as the module's introduction, so nothing is copied or removed.
            migrationBuilder.Sql(@"
INSERT INTO module_items (Id, ModuleId, OfferingId, ItemType, TargetId, Title, Url, Body, Position, IndentLevel, IsPublished,
                          RequirementType, MinScorePercent, CreatedByUserId, CreatedAt)
SELECT NEWID(), v.ModuleId, m.OfferingId, 'Video', v.Id, v.Title, NULL, NULL,
       ROW_NUMBER() OVER (PARTITION BY v.ModuleId ORDER BY v.CreatedAt, v.Id), 0, 1, 'None', NULL,
       '00000000-0000-0000-0000-000000000000', SYSUTCDATETIME()
FROM content_videos v
JOIN course_content_modules m ON m.Id = v.ModuleId
WHERE v.IsDeleted = 0 AND m.IsDeleted = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "module_item_progress");

            migrationBuilder.DropTable(
                name: "module_prerequisites");

            migrationBuilder.DropTable(
                name: "module_items");

            migrationBuilder.DropColumn(
                name: "CompletionMode",
                table: "course_content_modules");

            migrationBuilder.DropColumn(
                name: "RequireSequentialProgress",
                table: "course_content_modules");

            migrationBuilder.DropColumn(
                name: "UnlockAt",
                table: "course_content_modules");
        }
    }
}
