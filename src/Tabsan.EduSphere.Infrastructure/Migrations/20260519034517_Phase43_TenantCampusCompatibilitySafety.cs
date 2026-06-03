using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase43_TenantCampusCompatibilitySafety : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_departments_campuses_CampusId",
                table: "departments");

            migrationBuilder.DropForeignKey(
                name: "FK_users_campuses_CampusId",
                table: "users");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_campuses_Id_TenantId",
                table: "campuses",
                columns: new[] { "Id", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_users_CampusId_TenantId",
                table: "users",
                columns: new[] { "CampusId", "TenantId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_tenant_campus_pair",
                table: "users",
                sql: "([TenantId] IS NULL AND [CampusId] IS NULL) OR ([TenantId] IS NOT NULL AND [CampusId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_departments_CampusId_TenantId",
                table: "departments",
                columns: new[] { "CampusId", "TenantId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_departments_tenant_campus_pair",
                table: "departments",
                sql: "([TenantId] IS NULL AND [CampusId] IS NULL) OR ([TenantId] IS NOT NULL AND [CampusId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_departments_campuses_CampusId_TenantId",
                table: "departments",
                columns: new[] { "CampusId", "TenantId" },
                principalTable: "campuses",
                principalColumns: new[] { "Id", "TenantId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_campuses_CampusId_TenantId",
                table: "users",
                columns: new[] { "CampusId", "TenantId" },
                principalTable: "campuses",
                principalColumns: new[] { "Id", "TenantId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_departments_campuses_CampusId_TenantId",
                table: "departments");

            migrationBuilder.DropForeignKey(
                name: "FK_users_campuses_CampusId_TenantId",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_CampusId_TenantId",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_users_tenant_campus_pair",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_departments_CampusId_TenantId",
                table: "departments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_departments_tenant_campus_pair",
                table: "departments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_campuses_Id_TenantId",
                table: "campuses");

            migrationBuilder.AddForeignKey(
                name: "FK_departments_campuses_CampusId",
                table: "departments",
                column: "CampusId",
                principalTable: "campuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_campuses_CampusId",
                table: "users",
                column: "CampusId",
                principalTable: "campuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
