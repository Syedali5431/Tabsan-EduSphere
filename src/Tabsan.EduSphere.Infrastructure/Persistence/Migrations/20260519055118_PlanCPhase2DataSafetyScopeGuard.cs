using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlanCPhase2DataSafetyScopeGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_course_materials_location_by_type",
                table: "course_materials",
                sql: "([MaterialType] = 1 AND [FilePath] IS NOT NULL AND LTRIM(RTRIM([FilePath])) <> '') OR ([MaterialType] = 2 AND [LinkUrl] IS NOT NULL AND LTRIM(RTRIM([LinkUrl])) <> '') OR ([MaterialType] = 3 AND (([FilePath] IS NOT NULL AND LTRIM(RTRIM([FilePath])) <> '') OR ([LinkUrl] IS NOT NULL AND LTRIM(RTRIM([LinkUrl])) <> '')))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_course_materials_material_type",
                table: "course_materials",
                sql: "[MaterialType] IN (1,2,3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_course_materials_scope_required",
                table: "course_materials",
                sql: "[TenantId] <> '00000000-0000-0000-0000-000000000000' AND [CampusId] <> '00000000-0000-0000-0000-000000000000' AND [DepartmentId] <> '00000000-0000-0000-0000-000000000000' AND [AcademicProgramId] <> '00000000-0000-0000-0000-000000000000' AND [SemesterId] <> '00000000-0000-0000-0000-000000000000' AND [CourseId] <> '00000000-0000-0000-0000-000000000000' AND [CreatedByUserId] <> '00000000-0000-0000-0000-000000000000'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_course_materials_location_by_type",
                table: "course_materials");

            migrationBuilder.DropCheckConstraint(
                name: "CK_course_materials_material_type",
                table: "course_materials");

            migrationBuilder.DropCheckConstraint(
                name: "CK_course_materials_scope_required",
                table: "course_materials");
        }
    }
}
