using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase46_TenantCampusQueryOptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_users_tenant_campus_active_role",
                table: "users",
                columns: new[] { "TenantId", "CampusId", "IsActive", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_users_tenant_campus_username",
                table: "users",
                columns: new[] { "TenantId", "CampusId", "Username" });

            migrationBuilder.CreateIndex(
                name: "IX_departments_tenant_campus_name",
                table: "departments",
                columns: new[] { "TenantId", "CampusId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_tenant_campus_active_role",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_tenant_campus_username",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_departments_tenant_campus_name",
                table: "departments");
        }
    }
}
