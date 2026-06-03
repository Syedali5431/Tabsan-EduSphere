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
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_departments_campuses_CampusId')
BEGIN
    ALTER TABLE [departments] DROP CONSTRAINT [FK_departments_campuses_CampusId];
END");

            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_users_campuses_CampusId')
BEGIN
    ALTER TABLE [users] DROP CONSTRAINT [FK_users_campuses_CampusId];
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('campuses', 'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE [name] = N'AK_campuses_Id_TenantId'
      AND [parent_object_id] = OBJECT_ID(N'campuses'))
BEGIN
    ALTER TABLE [campuses] ADD CONSTRAINT [AK_campuses_Id_TenantId] UNIQUE ([Id], [TenantId]);
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('users', 'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_users_CampusId_TenantId'
      AND [object_id] = OBJECT_ID(N'users'))
BEGIN
    CREATE INDEX [IX_users_CampusId_TenantId] ON [users] ([CampusId], [TenantId]);
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('users', 'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_users_tenant_campus_pair'
      AND [parent_object_id] = OBJECT_ID(N'users'))
BEGIN
    ALTER TABLE [users] ADD CONSTRAINT [CK_users_tenant_campus_pair]
    CHECK (([TenantId] IS NULL AND [CampusId] IS NULL) OR ([TenantId] IS NOT NULL AND [CampusId] IS NOT NULL));
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('departments', 'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_departments_CampusId_TenantId'
      AND [object_id] = OBJECT_ID(N'departments'))
BEGIN
    CREATE INDEX [IX_departments_CampusId_TenantId] ON [departments] ([CampusId], [TenantId]);
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('departments', 'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_departments_tenant_campus_pair'
      AND [parent_object_id] = OBJECT_ID(N'departments'))
BEGIN
    ALTER TABLE [departments] ADD CONSTRAINT [CK_departments_tenant_campus_pair]
    CHECK (([TenantId] IS NULL AND [CampusId] IS NULL) OR ([TenantId] IS NOT NULL AND [CampusId] IS NOT NULL));
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('departments', 'U') IS NOT NULL
AND OBJECT_ID('campuses', 'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_departments_campuses_CampusId_TenantId')
BEGIN
    ALTER TABLE [departments] ADD CONSTRAINT [FK_departments_campuses_CampusId_TenantId]
    FOREIGN KEY ([CampusId], [TenantId]) REFERENCES [campuses]([Id], [TenantId]) ON DELETE NO ACTION;
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('users', 'U') IS NOT NULL
AND OBJECT_ID('campuses', 'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_users_campuses_CampusId_TenantId')
BEGIN
    ALTER TABLE [users] ADD CONSTRAINT [FK_users_campuses_CampusId_TenantId]
    FOREIGN KEY ([CampusId], [TenantId]) REFERENCES [campuses]([Id], [TenantId]) ON DELETE NO ACTION;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_departments_campuses_CampusId_TenantId')
BEGIN
    ALTER TABLE [departments] DROP CONSTRAINT [FK_departments_campuses_CampusId_TenantId];
END");

            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_users_campuses_CampusId_TenantId')
BEGIN
    ALTER TABLE [users] DROP CONSTRAINT [FK_users_campuses_CampusId_TenantId];
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('users', 'U') IS NOT NULL
AND EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_users_CampusId_TenantId'
      AND [object_id] = OBJECT_ID(N'users'))
BEGIN
    DROP INDEX [IX_users_CampusId_TenantId] ON [users];
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('users', 'U') IS NOT NULL
AND EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_users_tenant_campus_pair'
      AND [parent_object_id] = OBJECT_ID(N'users'))
BEGIN
    ALTER TABLE [users] DROP CONSTRAINT [CK_users_tenant_campus_pair];
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('departments', 'U') IS NOT NULL
AND EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_departments_CampusId_TenantId'
      AND [object_id] = OBJECT_ID(N'departments'))
BEGIN
    DROP INDEX [IX_departments_CampusId_TenantId] ON [departments];
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('departments', 'U') IS NOT NULL
AND EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_departments_tenant_campus_pair'
      AND [parent_object_id] = OBJECT_ID(N'departments'))
BEGIN
    ALTER TABLE [departments] DROP CONSTRAINT [CK_departments_tenant_campus_pair];
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('campuses', 'U') IS NOT NULL
AND EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE [name] = N'AK_campuses_Id_TenantId'
      AND [parent_object_id] = OBJECT_ID(N'campuses'))
BEGIN
    ALTER TABLE [campuses] DROP CONSTRAINT [AK_campuses_Id_TenantId];
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('departments', 'U') IS NOT NULL
AND OBJECT_ID('campuses', 'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_departments_campuses_CampusId')
BEGIN
    ALTER TABLE [departments] ADD CONSTRAINT [FK_departments_campuses_CampusId]
    FOREIGN KEY ([CampusId]) REFERENCES [campuses]([Id]) ON DELETE NO ACTION;
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('users', 'U') IS NOT NULL
AND OBJECT_ID('campuses', 'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_users_campuses_CampusId')
BEGIN
    ALTER TABLE [users] ADD CONSTRAINT [FK_users_campuses_CampusId]
    FOREIGN KEY ([CampusId]) REFERENCES [campuses]([Id]) ON DELETE NO ACTION;
END");
        }
    }
}
