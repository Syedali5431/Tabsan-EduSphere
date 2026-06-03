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
            migrationBuilder.Sql(@"IF OBJECT_ID('users', 'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_users_tenant_campus_active_role'
      AND [object_id] = OBJECT_ID(N'users'))
BEGIN
    CREATE INDEX [IX_users_tenant_campus_active_role] ON [users] ([TenantId], [CampusId], [IsActive], [RoleId]);
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('users', 'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_users_tenant_campus_username'
      AND [object_id] = OBJECT_ID(N'users'))
BEGIN
    CREATE INDEX [IX_users_tenant_campus_username] ON [users] ([TenantId], [CampusId], [Username]);
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('departments', 'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_departments_tenant_campus_name'
      AND [object_id] = OBJECT_ID(N'departments'))
BEGIN
    CREATE INDEX [IX_departments_tenant_campus_name] ON [departments] ([TenantId], [CampusId], [Name]);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID('users', 'U') IS NOT NULL
AND EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_users_tenant_campus_active_role'
      AND [object_id] = OBJECT_ID(N'users'))
BEGIN
    DROP INDEX [IX_users_tenant_campus_active_role] ON [users];
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('users', 'U') IS NOT NULL
AND EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_users_tenant_campus_username'
      AND [object_id] = OBJECT_ID(N'users'))
BEGIN
    DROP INDEX [IX_users_tenant_campus_username] ON [users];
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('departments', 'U') IS NOT NULL
AND EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_departments_tenant_campus_name'
      AND [object_id] = OBJECT_ID(N'departments'))
BEGIN
    DROP INDEX [IX_departments_tenant_campus_name] ON [departments];
END");
        }
    }
}
