using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase50_DepartmentTenantCampusScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_departments_code",
                table: "departments");

            migrationBuilder.CreateIndex(
                name: "IX_departments_scope_code",
                table: "departments",
                columns: new[] { "TenantId", "CampusId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL AND [CampusId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_departments_scope_code",
                table: "departments");

            migrationBuilder.CreateIndex(
                name: "IX_departments_code",
                table: "departments",
                column: "Code",
                unique: true);
        }
    }
}
