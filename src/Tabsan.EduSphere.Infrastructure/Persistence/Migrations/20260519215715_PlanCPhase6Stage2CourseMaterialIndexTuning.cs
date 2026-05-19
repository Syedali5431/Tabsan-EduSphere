using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlanCPhase6Stage2CourseMaterialIndexTuning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_course_materials_scope_active_sort",
                table: "course_materials",
                columns: new[] { "TenantId", "CampusId", "IsActive", "Name", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_course_materials_scope_active_sort",
                table: "course_materials");
        }
    }
}
