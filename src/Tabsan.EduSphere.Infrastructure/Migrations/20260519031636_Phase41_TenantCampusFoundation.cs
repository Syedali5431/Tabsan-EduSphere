using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase41_TenantCampusFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF COL_LENGTH('users', 'CampusId') IS NULL
BEGIN
    ALTER TABLE [users] ADD [CampusId] uniqueidentifier NULL;
END");

            migrationBuilder.Sql(@"IF COL_LENGTH('users', 'TenantId') IS NULL
BEGIN
    ALTER TABLE [users] ADD [TenantId] uniqueidentifier NULL;
END");

            migrationBuilder.Sql(@"IF COL_LENGTH('departments', 'CampusId') IS NULL
BEGIN
    ALTER TABLE [departments] ADD [CampusId] uniqueidentifier NULL;
END");

            migrationBuilder.Sql(@"IF COL_LENGTH('departments', 'TenantId') IS NULL
BEGIN
    ALTER TABLE [departments] ADD [TenantId] uniqueidentifier NULL;
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('tenants', 'U') IS NULL
BEGIN
    CREATE TABLE [tenants] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_tenants_IsActive] DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_tenants] PRIMARY KEY ([Id])
    );
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('campuses', 'U') IS NULL
BEGIN
    CREATE TABLE [campuses] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_campuses_IsActive] DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_campuses] PRIMARY KEY ([Id])
    );
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('FK_campuses_tenants_TenantId', 'F') IS NULL
AND OBJECT_ID('campuses', 'U') IS NOT NULL
AND OBJECT_ID('tenants', 'U') IS NOT NULL
BEGIN
    ALTER TABLE [campuses] ADD CONSTRAINT [FK_campuses_tenants_TenantId]
    FOREIGN KEY ([TenantId]) REFERENCES [tenants] ([Id]) ON DELETE NO ACTION;
END");

            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_users_campus_id' AND object_id = OBJECT_ID('users'))
BEGIN
    CREATE INDEX [IX_users_campus_id] ON [users]([CampusId]);
END");

            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_users_tenant_id' AND object_id = OBJECT_ID('users'))
BEGIN
    CREATE INDEX [IX_users_tenant_id] ON [users]([TenantId]);
END");

            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_departments_campus_id' AND object_id = OBJECT_ID('departments'))
BEGIN
    CREATE INDEX [IX_departments_campus_id] ON [departments]([CampusId]);
END");

            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_departments_tenant_id' AND object_id = OBJECT_ID('departments'))
BEGIN
    CREATE INDEX [IX_departments_tenant_id] ON [departments]([TenantId]);
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('campuses', 'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_campuses_tenant_code' AND object_id = OBJECT_ID('campuses'))
BEGIN
    CREATE UNIQUE INDEX [IX_campuses_tenant_code] ON [campuses]([TenantId], [Code]);
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('campuses', 'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_campuses_tenant_id' AND object_id = OBJECT_ID('campuses'))
BEGIN
    CREATE INDEX [IX_campuses_tenant_id] ON [campuses]([TenantId]);
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('tenants', 'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tenants_code' AND object_id = OBJECT_ID('tenants'))
BEGIN
    CREATE UNIQUE INDEX [IX_tenants_code] ON [tenants]([Code]);
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('FK_departments_campuses_CampusId', 'F') IS NULL
AND OBJECT_ID('departments', 'U') IS NOT NULL
AND OBJECT_ID('campuses', 'U') IS NOT NULL
BEGIN
    ALTER TABLE [departments] ADD CONSTRAINT [FK_departments_campuses_CampusId]
    FOREIGN KEY ([CampusId]) REFERENCES [campuses] ([Id]) ON DELETE NO ACTION;
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('FK_departments_tenants_TenantId', 'F') IS NULL
AND OBJECT_ID('departments', 'U') IS NOT NULL
AND OBJECT_ID('tenants', 'U') IS NOT NULL
BEGIN
    ALTER TABLE [departments] ADD CONSTRAINT [FK_departments_tenants_TenantId]
    FOREIGN KEY ([TenantId]) REFERENCES [tenants] ([Id]) ON DELETE NO ACTION;
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('FK_users_campuses_CampusId', 'F') IS NULL
AND OBJECT_ID('users', 'U') IS NOT NULL
AND OBJECT_ID('campuses', 'U') IS NOT NULL
BEGIN
    ALTER TABLE [users] ADD CONSTRAINT [FK_users_campuses_CampusId]
    FOREIGN KEY ([CampusId]) REFERENCES [campuses] ([Id]) ON DELETE NO ACTION;
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('FK_users_tenants_TenantId', 'F') IS NULL
AND OBJECT_ID('users', 'U') IS NOT NULL
AND OBJECT_ID('tenants', 'U') IS NOT NULL
BEGIN
    ALTER TABLE [users] ADD CONSTRAINT [FK_users_tenants_TenantId]
    FOREIGN KEY ([TenantId]) REFERENCES [tenants] ([Id]) ON DELETE NO ACTION;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID('FK_departments_campuses_CampusId', 'F') IS NOT NULL
BEGIN
    ALTER TABLE [departments] DROP CONSTRAINT [FK_departments_campuses_CampusId];
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('FK_departments_tenants_TenantId', 'F') IS NOT NULL
BEGIN
    ALTER TABLE [departments] DROP CONSTRAINT [FK_departments_tenants_TenantId];
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('FK_users_campuses_CampusId', 'F') IS NOT NULL
BEGIN
    ALTER TABLE [users] DROP CONSTRAINT [FK_users_campuses_CampusId];
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('FK_users_tenants_TenantId', 'F') IS NOT NULL
BEGIN
    ALTER TABLE [users] DROP CONSTRAINT [FK_users_tenants_TenantId];
END");

            migrationBuilder.Sql(@"IF OBJECT_ID('campuses', 'U') IS NOT NULL DROP TABLE [campuses];");
            migrationBuilder.Sql(@"IF OBJECT_ID('tenants', 'U') IS NOT NULL DROP TABLE [tenants];");

            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_users_campus_id' AND object_id = OBJECT_ID('users'))
BEGIN
    DROP INDEX [IX_users_campus_id] ON [users];
END");

            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_users_tenant_id' AND object_id = OBJECT_ID('users'))
BEGIN
    DROP INDEX [IX_users_tenant_id] ON [users];
END");

            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_departments_campus_id' AND object_id = OBJECT_ID('departments'))
BEGIN
    DROP INDEX [IX_departments_campus_id] ON [departments];
END");

            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_departments_tenant_id' AND object_id = OBJECT_ID('departments'))
BEGIN
    DROP INDEX [IX_departments_tenant_id] ON [departments];
END");

            migrationBuilder.Sql(@"IF COL_LENGTH('users', 'CampusId') IS NOT NULL
BEGIN
    ALTER TABLE [users] DROP COLUMN [CampusId];
END");

            migrationBuilder.Sql(@"IF COL_LENGTH('users', 'TenantId') IS NOT NULL
BEGIN
    ALTER TABLE [users] DROP COLUMN [TenantId];
END");

            migrationBuilder.Sql(@"IF COL_LENGTH('departments', 'CampusId') IS NOT NULL
BEGIN
    ALTER TABLE [departments] DROP COLUMN [CampusId];
END");

            migrationBuilder.Sql(@"IF COL_LENGTH('departments', 'TenantId') IS NOT NULL
BEGIN
    ALTER TABLE [departments] DROP COLUMN [TenantId];
END");
        }
    }
}
