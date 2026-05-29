/*
  Schema creation script for Tabsan EduSphere.
  
  SCRIPT EXECUTION ORDER (IMPORTANT):
  1. 01-Schema-Current.sql      - This script: creates all tables and schema (RUN THIS FIRST)
  2. 02-Seed-Core.sql           - Seeds core data (roles, institutions, departments, users)
  3. 03-FullDummyData.sql       - Adds comprehensive test data
  4. 04-Maintenance-Indexes-And-Views.sql - Creates indexes and views (optional)
  5. 05-PostDeployment-Checks.sql - Validates data integrity (optional)

  PURPOSE:
  - Creates the complete database schema
  - Creates all tables with proper relationships
  - Defines all foreign keys and constraints
  - Establishes indexes for performance

  NOTE:
  - Must run FIRST before any other scripts
  - This script is safe to run repeatedly (uses IF NOT EXISTS checks)
*/

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'Tabsan-EduSphere') IS NULL
BEGIN
    CREATE DATABASE [Tabsan-EduSphere];
END;
GO

USE [Tabsan-EduSphere];
GO

IF DB_NAME() <> N'Tabsan-EduSphere'
BEGIN
    RAISERROR('Failed to switch context to [Tabsan-EduSphere]. Aborting schema script.', 16, 1);
    RETURN;
END;
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

IF OBJECT_ID(N'[Tabsan-EduSphere]') IS NULL
BEGIN
    CREATE TABLE [Tabsan-EduSphere] (
        [Id] uniqueidentifier NOT NULL,
        [DemoKey] nvarchar(100) NOT NULL,
        [DemoValue] nvarchar(4000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Tabsan-EduSphere] PRIMARY KEY ([Id])
    );

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Tabsan-EduSphere_DemoKey' AND [object_id] = OBJECT_ID(N'[Tabsan-EduSphere]'))

    BEGIN

        CREATE UNIQUE INDEX [IX_Tabsan-EduSphere_DemoKey] ON [Tabsan-EduSphere] ([DemoKey]);

    END;
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE TABLE [audit_logs] (
        [Id] bigint NOT NULL IDENTITY,
        [ActorUserId] uniqueidentifier NULL,
        [Action] nvarchar(100) NOT NULL,
        [EntityName] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(100) NULL,
        [OldValuesJson] nvarchar(max) NULL,
        [NewValuesJson] nvarchar(max) NULL,
        [OccurredAt] datetime2 NOT NULL,
        [IpAddress] nvarchar(64) NULL,
        CONSTRAINT [PK_audit_logs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE TABLE [departments] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_departments] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE TABLE [license_state] (
        [Id] uniqueidentifier NOT NULL,
        [LicenseHash] nvarchar(128) NOT NULL,
        [LicenseType] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [ActivatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_license_state] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE TABLE [modules] (
        [Id] uniqueidentifier NOT NULL,
        [Key] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IsMandatory] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_modules] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE TABLE [roles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        [Description] nvarchar(256) NULL,
        [IsSystemRole] bit NOT NULL,
        CONSTRAINT [PK_roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE TABLE [module_status] (
        [Id] uniqueidentifier NOT NULL,
        [ModuleId] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL,
        [ActivatedAt] datetime2 NULL,
        [Source] nvarchar(20) NOT NULL,
        [ChangedBy] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_module_status] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_module_status_modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [modules] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE TABLE [users] (
        [Id] uniqueidentifier NOT NULL,
        [Username] nvarchar(100) NOT NULL,
        [Email] nvarchar(256) NULL,
        [PasswordHash] nvarchar(512) NOT NULL,
        [RoleId] int NOT NULL,
        [DepartmentId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL,
        [LastLoginAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_users_roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [roles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    CREATE TABLE [user_sessions] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [RefreshTokenHash] nvarchar(512) NOT NULL,
        [DeviceInfo] nvarchar(512) NULL,
        [IpAddress] nvarchar(64) NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_user_sessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_user_sessions_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_audit_logs_actor' AND [object_id] = OBJECT_ID(N'[audit_logs]'))
    BEGIN
        CREATE INDEX [IX_audit_logs_actor] ON [audit_logs] ([ActorUserId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_audit_logs_occurred_at' AND [object_id] = OBJECT_ID(N'[audit_logs]'))
    BEGIN
        CREATE INDEX [IX_audit_logs_occurred_at] ON [audit_logs] ([OccurredAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_departments_code' AND [object_id] = OBJECT_ID(N'[departments]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_departments_code] ON [departments] ([Code]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_module_status_module_id' AND [object_id] = OBJECT_ID(N'[module_status]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_module_status_module_id] ON [module_status] ([ModuleId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_modules_key' AND [object_id] = OBJECT_ID(N'[modules]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_modules_key] ON [modules] ([Key]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_roles_name' AND [object_id] = OBJECT_ID(N'[roles]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_roles_name] ON [roles] ([Name]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_user_sessions_token_hash' AND [object_id] = OBJECT_ID(N'[user_sessions]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_user_sessions_token_hash] ON [user_sessions] ([RefreshTokenHash]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_user_sessions_user_id' AND [object_id] = OBJECT_ID(N'[user_sessions]'))
    BEGIN
        CREATE INDEX [IX_user_sessions_user_id] ON [user_sessions] ([UserId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_users_email' AND [object_id] = OBJECT_ID(N'[users]'))
    BEGIN
        EXEC(N'CREATE UNIQUE INDEX [IX_users_email] ON [users] ([Email]) WHERE [email] IS NOT NULL');
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_users_RoleId' AND [object_id] = OBJECT_ID(N'[users]'))
    BEGIN
        CREATE INDEX [IX_users_RoleId] ON [users] ([RoleId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_users_username' AND [object_id] = OBJECT_ID(N'[users]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_users_username] ON [users] ([Username]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429002542_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429002542_InitialCreate', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525090000_Phase40_TenantCampusScope'
)
BEGIN
    CREATE TABLE [tenants] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_tenants] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525090000_Phase40_TenantCampusScope'
)
BEGIN
    CREATE TABLE [campuses] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_campuses] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525090000_Phase40_TenantCampusScope'
)
AND NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_campuses_tenants_TenantId'
)
BEGIN
    ALTER TABLE [campuses]
    ADD CONSTRAINT [FK_campuses_tenants_TenantId]
    FOREIGN KEY ([TenantId]) REFERENCES [tenants] ([Id]) ON DELETE NO ACTION;
END;
GO

IF COL_LENGTH('users', 'TenantId') IS NULL
BEGIN
    ALTER TABLE [users] ADD [TenantId] uniqueidentifier NULL;
END;
GO

IF COL_LENGTH('users', 'CampusId') IS NULL
BEGIN
    ALTER TABLE [users] ADD [CampusId] uniqueidentifier NULL;
END;
GO

IF COL_LENGTH('departments', 'TenantId') IS NULL
BEGIN
    ALTER TABLE [departments] ADD [TenantId] uniqueidentifier NULL;
END;
GO

IF COL_LENGTH('departments', 'CampusId') IS NULL
BEGIN
    ALTER TABLE [departments] ADD [CampusId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE [name] = N'AK_campuses_Id_TenantId'
)
AND OBJECT_ID(N'[campuses]') IS NOT NULL
BEGIN
    ALTER TABLE [campuses]
    ADD CONSTRAINT [AK_campuses_Id_TenantId] UNIQUE ([Id], [TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_users_tenant_campus_pair'
)
BEGIN
    ALTER TABLE [users]
    ADD CONSTRAINT [CK_users_tenant_campus_pair]
    CHECK (([TenantId] IS NULL AND [CampusId] IS NULL) OR ([TenantId] IS NOT NULL AND [CampusId] IS NOT NULL));
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_departments_tenant_campus_pair'
)
BEGIN
    ALTER TABLE [departments]
    ADD CONSTRAINT [CK_departments_tenant_campus_pair]
    CHECK (([TenantId] IS NULL AND [CampusId] IS NULL) OR ([TenantId] IS NOT NULL AND [CampusId] IS NOT NULL));
END;
GO

DECLARE @ScopeNow datetime2 = SYSUTCDATETIME();
DECLARE @DefaultTenantId uniqueidentifier = 'f1000000-0000-0000-0000-000000000001';
DECLARE @DefaultCampusId uniqueidentifier = 'f2000000-0000-0000-0000-000000000001';

IF OBJECT_ID(N'[tenants]') IS NOT NULL
BEGIN
    INSERT INTO [tenants] ([Id], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT @DefaultTenantId, N'DEFAULT', N'Default Tenant', 1, @ScopeNow, NULL, 0, NULL
    WHERE NOT EXISTS (SELECT 1 FROM [tenants] WHERE [Id] = @DefaultTenantId);
END;

IF OBJECT_ID(N'[campuses]') IS NOT NULL
BEGIN
    INSERT INTO [campuses] ([Id], [TenantId], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT @DefaultCampusId, @DefaultTenantId, N'MAIN', N'Main Campus', 1, @ScopeNow, NULL, 0, NULL
    WHERE NOT EXISTS (SELECT 1 FROM [campuses] WHERE [Id] = @DefaultCampusId);
END;
GO

UPDATE [users]
SET [TenantId] = 'f1000000-0000-0000-0000-000000000001',
    [CampusId] = 'f2000000-0000-0000-0000-000000000001'
WHERE ([TenantId] IS NULL AND [CampusId] IS NULL)
  AND [RoleId] <> (SELECT TOP 1 [Id] FROM [roles] WHERE [Name] = N'SuperAdmin');
GO

UPDATE [users]
SET [TenantId] = 'f1000000-0000-0000-0000-000000000001',
    [CampusId] = 'f2000000-0000-0000-0000-000000000001'
WHERE ([TenantId] IS NULL AND [CampusId] IS NOT NULL)
   OR ([TenantId] IS NOT NULL AND [CampusId] IS NULL);
GO

UPDATE [departments]
SET [TenantId] = 'f1000000-0000-0000-0000-000000000001',
    [CampusId] = 'f2000000-0000-0000-0000-000000000001'
WHERE ([TenantId] IS NULL AND [CampusId] IS NULL)
   OR ([TenantId] IS NULL AND [CampusId] IS NOT NULL)
   OR ([TenantId] IS NOT NULL AND [CampusId] IS NULL);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_users_tenants_TenantId')
BEGIN
    ALTER TABLE [users]
    ADD CONSTRAINT [FK_users_tenants_TenantId]
    FOREIGN KEY ([TenantId]) REFERENCES [tenants] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_departments_tenants_TenantId')
BEGIN
    ALTER TABLE [departments]
    ADD CONSTRAINT [FK_departments_tenants_TenantId]
    FOREIGN KEY ([TenantId]) REFERENCES [tenants] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_users_campuses_CampusId_TenantId')
BEGIN
    ALTER TABLE [users]
    ADD CONSTRAINT [FK_users_campuses_CampusId_TenantId]
    FOREIGN KEY ([CampusId], [TenantId]) REFERENCES [campuses] ([Id], [TenantId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_departments_campuses_CampusId_TenantId')
BEGIN
    ALTER TABLE [departments]
    ADD CONSTRAINT [FK_departments_campuses_CampusId_TenantId]
    FOREIGN KEY ([CampusId], [TenantId]) REFERENCES [campuses] ([Id], [TenantId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_tenants_code' AND [object_id] = OBJECT_ID(N'[tenants]'))
BEGIN
    CREATE UNIQUE INDEX [IX_tenants_code] ON [tenants] ([Code]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_campuses_tenant_code' AND [object_id] = OBJECT_ID(N'[campuses]'))
BEGIN
    CREATE UNIQUE INDEX [IX_campuses_tenant_code] ON [campuses] ([TenantId], [Code]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_campuses_tenant_id' AND [object_id] = OBJECT_ID(N'[campuses]'))
BEGIN
    CREATE INDEX [IX_campuses_tenant_id] ON [campuses] ([TenantId]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_users_tenant_id' AND [object_id] = OBJECT_ID(N'[users]'))
BEGIN
    CREATE INDEX [IX_users_tenant_id] ON [users] ([TenantId]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_users_campus_id' AND [object_id] = OBJECT_ID(N'[users]'))
BEGIN
    CREATE INDEX [IX_users_campus_id] ON [users] ([CampusId]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_users_tenant_campus_active_role' AND [object_id] = OBJECT_ID(N'[users]'))
BEGIN
    CREATE INDEX [IX_users_tenant_campus_active_role] ON [users] ([TenantId], [CampusId], [IsActive], [RoleId]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_users_tenant_campus_username' AND [object_id] = OBJECT_ID(N'[users]'))
BEGIN
    CREATE INDEX [IX_users_tenant_campus_username] ON [users] ([TenantId], [CampusId], [Username]);
END;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_departments_Code' AND [object_id] = OBJECT_ID(N'[departments]'))
BEGIN
    DROP INDEX [IX_departments_Code] ON [departments];
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_departments_scope_code' AND [object_id] = OBJECT_ID(N'[departments]'))
BEGIN
    CREATE UNIQUE INDEX [IX_departments_scope_code] ON [departments] ([TenantId], [CampusId], [Code]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_departments_tenant_id' AND [object_id] = OBJECT_ID(N'[departments]'))
BEGIN
    CREATE INDEX [IX_departments_tenant_id] ON [departments] ([TenantId]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_departments_campus_id' AND [object_id] = OBJECT_ID(N'[departments]'))
BEGIN
    CREATE INDEX [IX_departments_campus_id] ON [departments] ([CampusId]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_departments_tenant_campus_name' AND [object_id] = OBJECT_ID(N'[departments]'))
BEGIN
    CREATE INDEX [IX_departments_tenant_campus_name] ON [departments] ([TenantId], [CampusId], [Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260525090000_Phase40_TenantCampusScope'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260525090000_Phase40_TenantCampusScope', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

/* Phase 39.3 / 40.1 alignment: ensure MFA + phone columns exist on users */
IF COL_LENGTH('users', 'MfaIsEnabled') IS NULL
BEGIN
    ALTER TABLE [users] ADD [MfaIsEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF COL_LENGTH('users', 'MfaTotpSecret') IS NULL
BEGIN
    ALTER TABLE [users] ADD [MfaTotpSecret] nvarchar(128) NULL;
END;
GO

IF COL_LENGTH('users', 'MfaRecoveryCodesHashJson') IS NULL
BEGIN
    ALTER TABLE [users] ADD [MfaRecoveryCodesHashJson] nvarchar(4000) NULL;
END;
GO

IF COL_LENGTH('users', 'PhoneNumber') IS NULL
BEGIN
    ALTER TABLE [users] ADD [PhoneNumber] nvarchar(32) NULL;
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513121000_Phase1Stage11DepartmentInstitutionType'
)
BEGIN
    IF COL_LENGTH('departments', 'InstitutionType') IS NULL
    BEGIN
        ALTER TABLE [departments]
        ADD [InstitutionType] int NOT NULL
            CONSTRAINT [DF_departments_institution_type] DEFAULT(0);
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE [name] = N'IX_departments_institution_type'
          AND [object_id] = OBJECT_ID(N'departments')
    )
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_departments_institution_type' AND [object_id] = OBJECT_ID(N'[departments]'))
        BEGIN
            CREATE INDEX [IX_departments_institution_type] ON [departments] ([InstitutionType]);
        END;
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513121000_Phase1Stage11DepartmentInstitutionType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513121000_Phase1Stage11DepartmentInstitutionType', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes'
)
BEGIN
    IF OBJECT_ID(N'[enrollments]') IS NOT NULL
    AND COL_LENGTH('enrollments', 'Status') IS NOT NULL
    BEGIN
        ALTER TABLE [enrollments] ALTER COLUMN [Status] nvarchar(32) NOT NULL;
    END;

    IF OBJECT_ID(N'[academic_programs]') IS NOT NULL
    AND EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE [name] = N'IX_academic_programs_code'
          AND [object_id] = OBJECT_ID(N'academic_programs')
    )
    BEGIN
        DROP INDEX [IX_academic_programs_code] ON [academic_programs];
    END;

        IF OBJECT_ID(N'[academic_programs]') IS NOT NULL
        AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE [name] = N'IX_academic_programs_code_dept'
          AND [object_id] = OBJECT_ID(N'academic_programs')
    )
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_academic_programs_code_dept' AND [object_id] = OBJECT_ID(N'[academic_programs]'))
        BEGIN
            CREATE UNIQUE INDEX [IX_academic_programs_code_dept] ON [academic_programs] ([Code], [DepartmentId]);
        END;
    END;

    IF OBJECT_ID(N'[academic_programs]') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_academic_programs_dept_active' AND [object_id] = OBJECT_ID(N'academic_programs'))
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_academic_programs_dept_active' AND [object_id] = OBJECT_ID(N'[academic_programs]'))
        BEGIN
            CREATE INDEX [IX_academic_programs_dept_active] ON [academic_programs] ([DepartmentId], [IsActive]);
        END;
    IF OBJECT_ID(N'[courses]') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_courses_dept_active' AND [object_id] = OBJECT_ID(N'courses'))
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_courses_dept_active' AND [object_id] = OBJECT_ID(N'[courses]'))
        BEGIN
            CREATE INDEX [IX_courses_dept_active] ON [courses] ([DepartmentId], [IsActive]);
        END;
    IF OBJECT_ID(N'[course_offerings]') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_course_offerings_semester_open' AND [object_id] = OBJECT_ID(N'course_offerings'))
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_course_offerings_semester_open' AND [object_id] = OBJECT_ID(N'[course_offerings]'))
        BEGIN
            CREATE INDEX [IX_course_offerings_semester_open] ON [course_offerings] ([SemesterId], [IsOpen]);
        END;
    IF OBJECT_ID(N'[course_offerings]') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_course_offerings_faculty_open' AND [object_id] = OBJECT_ID(N'course_offerings'))
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_course_offerings_faculty_open' AND [object_id] = OBJECT_ID(N'[course_offerings]'))
        BEGIN
            CREATE INDEX [IX_course_offerings_faculty_open] ON [course_offerings] ([FacultyUserId], [IsOpen]);
        END;
    IF OBJECT_ID(N'[student_profiles]') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_student_profiles_dept_status' AND [object_id] = OBJECT_ID(N'student_profiles'))
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_student_profiles_dept_status' AND [object_id] = OBJECT_ID(N'[student_profiles]'))
        BEGIN
            CREATE INDEX [IX_student_profiles_dept_status] ON [student_profiles] ([DepartmentId], [Status]);
        END;
    IF OBJECT_ID(N'[student_profiles]') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_student_profiles_program_status' AND [object_id] = OBJECT_ID(N'student_profiles'))
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_student_profiles_program_status' AND [object_id] = OBJECT_ID(N'[student_profiles]'))
        BEGIN
            CREATE INDEX [IX_student_profiles_program_status] ON [student_profiles] ([ProgramId], [Status]);
        END;
    IF OBJECT_ID(N'[enrollments]') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_enrollments_offering_status' AND [object_id] = OBJECT_ID(N'enrollments'))
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_enrollments_offering_status' AND [object_id] = OBJECT_ID(N'[enrollments]'))
        BEGIN
            CREATE INDEX [IX_enrollments_offering_status] ON [enrollments] ([CourseOfferingId], [Status]);
        END;
    IF OBJECT_ID(N'[enrollments]') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_enrollments_student_status' AND [object_id] = OBJECT_ID(N'enrollments'))
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_enrollments_student_status' AND [object_id] = OBJECT_ID(N'[enrollments]'))
        BEGIN
            CREATE INDEX [IX_enrollments_student_status] ON [enrollments] ([StudentProfileId], [Status]);
        END;
    IF OBJECT_ID(N'[faculty_department_assignments]') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_faculty_dept_assignments_active_lookup' AND [object_id] = OBJECT_ID(N'faculty_department_assignments'))
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_faculty_dept_assignments_active_lookup' AND [object_id] = OBJECT_ID(N'[faculty_department_assignments]'))
        BEGIN
            CREATE INDEX [IX_faculty_dept_assignments_active_lookup] ON [faculty_department_assignments] ([FacultyUserId], [RemovedAt], [DepartmentId]);
        END;
    IF OBJECT_ID(N'[admin_department_assignments]') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_admin_dept_assignments_active_lookup' AND [object_id] = OBJECT_ID(N'admin_department_assignments'))
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_admin_dept_assignments_active_lookup' AND [object_id] = OBJECT_ID(N'[admin_department_assignments]'))
        BEGIN
            CREATE INDEX [IX_admin_dept_assignments_active_lookup] ON [admin_department_assignments] ([AdminUserId], [RemovedAt], [DepartmentId]);
        END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE TABLE [academic_programs] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [TotalSemesters] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_academic_programs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_academic_programs_departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [departments] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523021000_Phase47_CourseInstitutionScope'
)
BEGIN
    IF COL_LENGTH('courses', 'CampusId') IS NULL
        ALTER TABLE [courses] ADD [CampusId] uniqueidentifier NULL;

    IF COL_LENGTH('courses', 'InstitutionType') IS NULL
        ALTER TABLE [courses] ADD [InstitutionType] int NOT NULL CONSTRAINT [DF_courses_InstitutionType] DEFAULT(3);

    IF COL_LENGTH('courses', 'TenantId') IS NULL
        ALTER TABLE [courses] ADD [TenantId] uniqueidentifier NULL;

    IF COL_LENGTH('course_offerings', 'CampusId') IS NULL
        ALTER TABLE [course_offerings] ADD [CampusId] uniqueidentifier NULL;

    IF COL_LENGTH('course_offerings', 'InstitutionType') IS NULL
        ALTER TABLE [course_offerings] ADD [InstitutionType] int NOT NULL CONSTRAINT [DF_course_offerings_InstitutionType] DEFAULT(3);

    IF COL_LENGTH('course_offerings', 'TenantId') IS NULL
        ALTER TABLE [course_offerings] ADD [TenantId] uniqueidentifier NULL;

    UPDATE c
    SET
        c.[TenantId] = d.[TenantId],
        c.[CampusId] = d.[CampusId],
        c.[InstitutionType] = CAST(d.[InstitutionType] AS int)
    FROM [courses] c
    INNER JOIN [departments] d ON d.[Id] = c.[DepartmentId]
    WHERE c.[TenantId] IS NULL OR c.[CampusId] IS NULL OR c.[InstitutionType] = 3;

    UPDATE o
    SET
        o.[TenantId] = c.[TenantId],
        o.[CampusId] = c.[CampusId],
        o.[InstitutionType] = c.[InstitutionType]
    FROM [course_offerings] o
    INNER JOIN [courses] c ON c.[Id] = o.[CourseId]
    WHERE o.[TenantId] IS NULL OR o.[CampusId] IS NULL OR o.[InstitutionType] = 3;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_courses_scope_active' AND [object_id] = OBJECT_ID(N'[courses]'))
        CREATE INDEX [IX_courses_scope_active] ON [courses] ([TenantId], [CampusId], [InstitutionType], [IsActive]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_course_offerings_scope_open' AND [object_id] = OBJECT_ID(N'[course_offerings]'))
        CREATE INDEX [IX_course_offerings_scope_open] ON [course_offerings] ([TenantId], [CampusId], [InstitutionType], [IsOpen]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523021000_Phase47_CourseInstitutionScope'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260523021000_Phase47_CourseInstitutionScope', N'8.0.4');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE TABLE [courses] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [CreditHours] int NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_courses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_courses_departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [departments] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE TABLE [faculty_department_assignments] (
        [Id] uniqueidentifier NOT NULL,
        [FacultyUserId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        [RemovedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_faculty_department_assignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_faculty_department_assignments_departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [departments] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE TABLE [registration_whitelist] (
        [Id] uniqueidentifier NOT NULL,
        [IdentifierType] nvarchar(max) NOT NULL,
        [IdentifierValue] nvarchar(256) NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [ProgramId] uniqueidentifier NOT NULL,
        [IsUsed] bit NOT NULL,
        [UsedAt] datetime2 NULL,
        [CreatedUserId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_registration_whitelist] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE TABLE [semesters] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [IsClosed] bit NOT NULL,
        [ClosedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_semesters] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE TABLE [student_profiles] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [RegistrationNumber] nvarchar(50) NOT NULL,
        [ProgramId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [AdmissionDate] datetime2 NOT NULL,
        [Cgpa] decimal(4,2) NOT NULL,
        [CurrentSemesterNumber] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_student_profiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_student_profiles_academic_programs_ProgramId] FOREIGN KEY ([ProgramId]) REFERENCES [academic_programs] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_student_profiles_departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [departments] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE TABLE [course_offerings] (
        [Id] uniqueidentifier NOT NULL,
        [CourseId] uniqueidentifier NOT NULL,
        [SemesterId] uniqueidentifier NOT NULL,
        [FacultyUserId] uniqueidentifier NULL,
        [MaxEnrollment] int NOT NULL,
        [IsOpen] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_course_offerings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_course_offerings_courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [courses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_course_offerings_semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [semesters] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    CREATE TABLE [enrollments] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [EnrolledAt] datetime2 NOT NULL,
        [DroppedAt] datetime2 NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_enrollments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_enrollments_course_offerings_CourseOfferingId] FOREIGN KEY ([CourseOfferingId]) REFERENCES [course_offerings] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_enrollments_student_profiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [student_profiles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_academic_programs_code' AND [object_id] = OBJECT_ID(N'[academic_programs]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_academic_programs_code] ON [academic_programs] ([Code]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_academic_programs_DepartmentId' AND [object_id] = OBJECT_ID(N'[academic_programs]'))
    BEGIN
        CREATE INDEX [IX_academic_programs_DepartmentId] ON [academic_programs] ([DepartmentId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_course_offerings_course_semester' AND [object_id] = OBJECT_ID(N'[course_offerings]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_course_offerings_course_semester] ON [course_offerings] ([CourseId], [SemesterId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_course_offerings_SemesterId' AND [object_id] = OBJECT_ID(N'[course_offerings]'))
    BEGIN
        CREATE INDEX [IX_course_offerings_SemesterId] ON [course_offerings] ([SemesterId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_courses_code_dept' AND [object_id] = OBJECT_ID(N'[courses]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_courses_code_dept] ON [courses] ([Code], [DepartmentId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_courses_DepartmentId' AND [object_id] = OBJECT_ID(N'[courses]'))
    BEGIN
        CREATE INDEX [IX_courses_DepartmentId] ON [courses] ([DepartmentId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_enrollments_CourseOfferingId' AND [object_id] = OBJECT_ID(N'[enrollments]'))
    BEGIN
        CREATE INDEX [IX_enrollments_CourseOfferingId] ON [enrollments] ([CourseOfferingId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_enrollments_student_offering' AND [object_id] = OBJECT_ID(N'[enrollments]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_enrollments_student_offering] ON [enrollments] ([StudentProfileId], [CourseOfferingId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_faculty_department_assignments_DepartmentId' AND [object_id] = OBJECT_ID(N'[faculty_department_assignments]'))
    BEGIN
        CREATE INDEX [IX_faculty_department_assignments_DepartmentId] ON [faculty_department_assignments] ([DepartmentId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_faculty_dept_assignments_faculty_dept' AND [object_id] = OBJECT_ID(N'[faculty_department_assignments]'))
    BEGIN
        CREATE INDEX [IX_faculty_dept_assignments_faculty_dept] ON [faculty_department_assignments] ([FacultyUserId], [DepartmentId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_registration_whitelist_identifier' AND [object_id] = OBJECT_ID(N'[registration_whitelist]'))
    BEGIN
        CREATE INDEX [IX_registration_whitelist_identifier] ON [registration_whitelist] ([IdentifierValue]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_student_profiles_DepartmentId' AND [object_id] = OBJECT_ID(N'[student_profiles]'))
    BEGIN
        CREATE INDEX [IX_student_profiles_DepartmentId] ON [student_profiles] ([DepartmentId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_student_profiles_ProgramId' AND [object_id] = OBJECT_ID(N'[student_profiles]'))
    BEGIN
        CREATE INDEX [IX_student_profiles_ProgramId] ON [student_profiles] ([ProgramId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_student_profiles_reg_no' AND [object_id] = OBJECT_ID(N'[student_profiles]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_student_profiles_reg_no] ON [student_profiles] ([RegistrationNumber]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_student_profiles_user_id' AND [object_id] = OBJECT_ID(N'[student_profiles]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_student_profiles_user_id] ON [student_profiles] ([UserId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429004340_AcademicCore'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429004340_AcademicCore', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    CREATE TABLE [assignments] (
        [Id] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [Description] nvarchar(4000) NULL,
        [DueDate] datetime2 NOT NULL,
        [MaxMarks] decimal(8,2) NOT NULL,
        [IsPublished] bit NOT NULL,
        [PublishedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_assignments] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    CREATE TABLE [results] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [ResultType] nvarchar(450) NOT NULL,
        [MarksObtained] decimal(8,2) NOT NULL,
        [MaxMarks] decimal(8,2) NOT NULL,
        [IsPublished] bit NOT NULL,
        [PublishedAt] datetime2 NULL,
        [PublishedByUserId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_results] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    CREATE TABLE [transcript_export_logs] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [RequestedByUserId] uniqueidentifier NOT NULL,
        [ExportedAt] datetime2 NOT NULL,
        [DocumentUrl] nvarchar(2048) NULL,
        [Format] nvarchar(10) NOT NULL,
        [IpAddress] nvarchar(45) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_transcript_export_logs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    CREATE TABLE [assignment_submissions] (
        [Id] uniqueidentifier NOT NULL,
        [AssignmentId] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [FileUrl] nvarchar(2048) NULL,
        [TextContent] nvarchar(max) NULL,
        [SubmittedAt] datetime2 NOT NULL,
        [MarksAwarded] decimal(8,2) NULL,
        [Feedback] nvarchar(2000) NULL,
        [GradedAt] datetime2 NULL,
        [GradedByUserId] uniqueidentifier NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_assignment_submissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_assignment_submissions_assignments_AssignmentId] FOREIGN KEY ([AssignmentId]) REFERENCES [assignments] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_assignment_submissions_assignment_student' AND [object_id] = OBJECT_ID(N'[assignment_submissions]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_assignment_submissions_assignment_student] ON [assignment_submissions] ([AssignmentId], [StudentProfileId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_assignments_offering_id' AND [object_id] = OBJECT_ID(N'[assignments]'))
    BEGIN
        CREATE INDEX [IX_assignments_offering_id] ON [assignments] ([CourseOfferingId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_results_offering_id' AND [object_id] = OBJECT_ID(N'[results]'))
    BEGIN
        CREATE INDEX [IX_results_offering_id] ON [results] ([CourseOfferingId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_results_student_offering_type' AND [object_id] = OBJECT_ID(N'[results]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_results_student_offering_type] ON [results] ([StudentProfileId], [CourseOfferingId], [ResultType]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_transcript_export_logs_student_id' AND [object_id] = OBJECT_ID(N'[transcript_export_logs]'))
    BEGIN
        CREATE INDEX [IX_transcript_export_logs_student_id] ON [transcript_export_logs] ([StudentProfileId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429005740_AssignmentsAndResults'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429005740_AssignmentsAndResults', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    CREATE TABLE [attendance_records] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [Date] datetime2 NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [MarkedByUserId] uniqueidentifier NOT NULL,
        [Remarks] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_attendance_records] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    CREATE TABLE [notifications] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [SenderUserId] uniqueidentifier NULL,
        [IsSystemGenerated] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_notifications] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    CREATE TABLE [notification_recipients] (
        [Id] uniqueidentifier NOT NULL,
        [NotificationId] uniqueidentifier NOT NULL,
        [RecipientUserId] uniqueidentifier NOT NULL,
        [IsRead] bit NOT NULL,
        [ReadAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_notification_recipients] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_notification_recipients_notifications_NotificationId] FOREIGN KEY ([NotificationId]) REFERENCES [notifications] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_attendance_offering_date' AND [object_id] = OBJECT_ID(N'[attendance_records]'))
    BEGIN
        CREATE INDEX [IX_attendance_offering_date] ON [attendance_records] ([CourseOfferingId], [Date]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_attendance_student_id' AND [object_id] = OBJECT_ID(N'[attendance_records]'))
    BEGIN
        CREATE INDEX [IX_attendance_student_id] ON [attendance_records] ([StudentProfileId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_attendance_student_offering_date' AND [object_id] = OBJECT_ID(N'[attendance_records]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_attendance_student_offering_date] ON [attendance_records] ([StudentProfileId], [CourseOfferingId], [Date]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_notification_recipients_notification_user' AND [object_id] = OBJECT_ID(N'[notification_recipients]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_notification_recipients_notification_user] ON [notification_recipients] ([NotificationId], [RecipientUserId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_notification_recipients_user_read' AND [object_id] = OBJECT_ID(N'[notification_recipients]'))
    BEGIN
        CREATE INDEX [IX_notification_recipients_user_read] ON [notification_recipients] ([RecipientUserId], [IsRead]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_notifications_sender_id' AND [object_id] = OBJECT_ID(N'[notifications]'))
    BEGIN
        CREATE INDEX [IX_notifications_sender_id] ON [notifications] ([SenderUserId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_notifications_type' AND [object_id] = OBJECT_ID(N'[notifications]'))
    BEGIN
        CREATE INDEX [IX_notifications_type] ON [notifications] ([Type]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429011542_NotificationsAndAttendance'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429011542_NotificationsAndAttendance', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE TABLE [fyp_projects] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [Title] nvarchar(500) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [SupervisorUserId] uniqueidentifier NULL,
        [CoordinatorRemarks] nvarchar(2000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_fyp_projects] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE TABLE [quizzes] (
        [Id] uniqueidentifier NOT NULL,
        [CourseOfferingId] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [Instructions] nvarchar(4000) NULL,
        [TimeLimitMinutes] int NULL,
        [MaxAttempts] int NOT NULL,
        [AvailableFrom] datetime2 NULL,
        [AvailableUntil] datetime2 NULL,
        [IsPublished] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_quizzes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE TABLE [fyp_meetings] (
        [Id] uniqueidentifier NOT NULL,
        [FypProjectId] uniqueidentifier NOT NULL,
        [ScheduledAt] datetime2 NOT NULL,
        [Venue] nvarchar(500) NOT NULL,
        [Agenda] nvarchar(4000) NULL,
        [Status] nvarchar(20) NOT NULL,
        [OrganiserUserId] uniqueidentifier NOT NULL,
        [Minutes] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_fyp_meetings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_fyp_meetings_fyp_projects_FypProjectId] FOREIGN KEY ([FypProjectId]) REFERENCES [fyp_projects] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE TABLE [fyp_panel_members] (
        [Id] uniqueidentifier NOT NULL,
        [FypProjectId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Role] nvarchar(20) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_fyp_panel_members] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_fyp_panel_members_fyp_projects_FypProjectId] FOREIGN KEY ([FypProjectId]) REFERENCES [fyp_projects] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE TABLE [quiz_attempts] (
        [Id] uniqueidentifier NOT NULL,
        [QuizId] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [StartedAt] datetime2 NOT NULL,
        [FinishedAt] datetime2 NULL,
        [Status] nvarchar(20) NOT NULL,
        [TotalScore] decimal(10,2) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_quiz_attempts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_quiz_attempts_quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [quizzes] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE TABLE [quiz_questions] (
        [Id] uniqueidentifier NOT NULL,
        [QuizId] uniqueidentifier NOT NULL,
        [Text] nvarchar(2000) NOT NULL,
        [Type] nvarchar(20) NOT NULL,
        [Marks] decimal(8,2) NOT NULL,
        [OrderIndex] int NOT NULL,
        [QuizId1] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_quiz_questions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_quiz_questions_quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [quizzes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_quiz_questions_quizzes_QuizId1] FOREIGN KEY ([QuizId1]) REFERENCES [quizzes] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE TABLE [quiz_answers] (
        [Id] uniqueidentifier NOT NULL,
        [QuizAttemptId] uniqueidentifier NOT NULL,
        [QuizQuestionId] uniqueidentifier NOT NULL,
        [SelectedOptionId] uniqueidentifier NULL,
        [TextResponse] nvarchar(4000) NULL,
        [MarksAwarded] decimal(8,2) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_quiz_answers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_quiz_answers_quiz_attempts_QuizAttemptId] FOREIGN KEY ([QuizAttemptId]) REFERENCES [quiz_attempts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_quiz_answers_quiz_questions_QuizQuestionId] FOREIGN KEY ([QuizQuestionId]) REFERENCES [quiz_questions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    CREATE TABLE [quiz_options] (
        [Id] uniqueidentifier NOT NULL,
        [QuizQuestionId] uniqueidentifier NOT NULL,
        [Text] nvarchar(1000) NOT NULL,
        [IsCorrect] bit NOT NULL,
        [OrderIndex] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_quiz_options] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_quiz_options_quiz_questions_QuizQuestionId] FOREIGN KEY ([QuizQuestionId]) REFERENCES [quiz_questions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_fyp_meetings_FypProjectId_ScheduledAt' AND [object_id] = OBJECT_ID(N'[fyp_meetings]'))
    BEGIN
        CREATE INDEX [IX_fyp_meetings_FypProjectId_ScheduledAt] ON [fyp_meetings] ([FypProjectId], [ScheduledAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_fyp_meetings_OrganiserUserId_Status' AND [object_id] = OBJECT_ID(N'[fyp_meetings]'))
    BEGIN
        CREATE INDEX [IX_fyp_meetings_OrganiserUserId_Status] ON [fyp_meetings] ([OrganiserUserId], [Status]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_fyp_panel_members_FypProjectId_UserId_Role' AND [object_id] = OBJECT_ID(N'[fyp_panel_members]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_fyp_panel_members_FypProjectId_UserId_Role] ON [fyp_panel_members] ([FypProjectId], [UserId], [Role]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_fyp_panel_members_UserId' AND [object_id] = OBJECT_ID(N'[fyp_panel_members]'))
    BEGIN
        CREATE INDEX [IX_fyp_panel_members_UserId] ON [fyp_panel_members] ([UserId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_fyp_projects_DepartmentId_Status' AND [object_id] = OBJECT_ID(N'[fyp_projects]'))
    BEGIN
        CREATE INDEX [IX_fyp_projects_DepartmentId_Status] ON [fyp_projects] ([DepartmentId], [Status]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_fyp_projects_StudentProfileId' AND [object_id] = OBJECT_ID(N'[fyp_projects]'))
    BEGIN
        CREATE INDEX [IX_fyp_projects_StudentProfileId] ON [fyp_projects] ([StudentProfileId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_fyp_projects_SupervisorUserId' AND [object_id] = OBJECT_ID(N'[fyp_projects]'))
    BEGIN
        CREATE INDEX [IX_fyp_projects_SupervisorUserId] ON [fyp_projects] ([SupervisorUserId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_quiz_answers_QuizAttemptId_QuizQuestionId' AND [object_id] = OBJECT_ID(N'[quiz_answers]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_quiz_answers_QuizAttemptId_QuizQuestionId] ON [quiz_answers] ([QuizAttemptId], [QuizQuestionId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_quiz_answers_QuizQuestionId' AND [object_id] = OBJECT_ID(N'[quiz_answers]'))
    BEGIN
        CREATE INDEX [IX_quiz_answers_QuizQuestionId] ON [quiz_answers] ([QuizQuestionId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_quiz_attempts_QuizId_StudentProfileId_Status' AND [object_id] = OBJECT_ID(N'[quiz_attempts]'))
    BEGIN
        CREATE INDEX [IX_quiz_attempts_QuizId_StudentProfileId_Status] ON [quiz_attempts] ([QuizId], [StudentProfileId], [Status]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_quiz_attempts_StudentProfileId' AND [object_id] = OBJECT_ID(N'[quiz_attempts]'))
    BEGIN
        CREATE INDEX [IX_quiz_attempts_StudentProfileId] ON [quiz_attempts] ([StudentProfileId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_quiz_options_QuizQuestionId_OrderIndex' AND [object_id] = OBJECT_ID(N'[quiz_options]'))
    BEGIN
        CREATE INDEX [IX_quiz_options_QuizQuestionId_OrderIndex] ON [quiz_options] ([QuizQuestionId], [OrderIndex]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_quiz_questions_QuizId_OrderIndex' AND [object_id] = OBJECT_ID(N'[quiz_questions]'))
    BEGIN
        CREATE INDEX [IX_quiz_questions_QuizId_OrderIndex] ON [quiz_questions] ([QuizId], [OrderIndex]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_quiz_questions_QuizId1' AND [object_id] = OBJECT_ID(N'[quiz_questions]'))
    BEGIN
        CREATE INDEX [IX_quiz_questions_QuizId1] ON [quiz_questions] ([QuizId1]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_quizzes_CourseOfferingId' AND [object_id] = OBJECT_ID(N'[quizzes]'))
    BEGIN
        CREATE INDEX [IX_quizzes_CourseOfferingId] ON [quizzes] ([CourseOfferingId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_quizzes_CourseOfferingId_IsPublished' AND [object_id] = OBJECT_ID(N'[quizzes]'))
    BEGIN
        CREATE INDEX [IX_quizzes_CourseOfferingId_IsPublished] ON [quizzes] ([CourseOfferingId], [IsPublished]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429013621_QuizzesAndFyp'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429013621_QuizzesAndFyp', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429035351_AiAndAnalytics'
)
BEGIN
    CREATE TABLE [chat_conversations] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [UserRole] nvarchar(50) NOT NULL,
        [DepartmentId] uniqueidentifier NULL,
        [StartedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_chat_conversations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429035351_AiAndAnalytics'
)
BEGIN
    CREATE TABLE [chat_messages] (
        [Id] uniqueidentifier NOT NULL,
        [ConversationId] uniqueidentifier NOT NULL,
        [Role] nvarchar(20) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [SentAt] datetime2 NOT NULL,
        [TokensUsed] int NOT NULL,
        CONSTRAINT [PK_chat_messages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_chat_messages_chat_conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [chat_conversations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429035351_AiAndAnalytics'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_chat_conversations_UserId' AND [object_id] = OBJECT_ID(N'[chat_conversations]'))
    BEGIN
        CREATE INDEX [IX_chat_conversations_UserId] ON [chat_conversations] ([UserId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429035351_AiAndAnalytics'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_chat_conversations_UserId_StartedAt' AND [object_id] = OBJECT_ID(N'[chat_conversations]'))
    BEGIN
        CREATE INDEX [IX_chat_conversations_UserId_StartedAt] ON [chat_conversations] ([UserId], [StartedAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429035351_AiAndAnalytics'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_chat_messages_ConversationId' AND [object_id] = OBJECT_ID(N'[chat_messages]'))
    BEGIN
        CREATE INDEX [IX_chat_messages_ConversationId] ON [chat_messages] ([ConversationId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429035351_AiAndAnalytics'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_chat_messages_ConversationId_SentAt' AND [object_id] = OBJECT_ID(N'[chat_messages]'))
    BEGIN
        CREATE INDEX [IX_chat_messages_ConversationId_SentAt] ON [chat_messages] ([ConversationId], [SentAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429035351_AiAndAnalytics'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429035351_AiAndAnalytics', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429041941_VerificationKeys'
)
BEGIN
    CREATE TABLE [consumed_verification_keys] (
        [Id] uniqueidentifier NOT NULL,
        [KeyHash] nvarchar(64) NOT NULL,
        [ConsumedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_consumed_verification_keys] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429041941_VerificationKeys'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_consumed_verification_keys_KeyHash' AND [object_id] = OBJECT_ID(N'[consumed_verification_keys]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_consumed_verification_keys_KeyHash] ON [consumed_verification_keys] ([KeyHash]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429041941_VerificationKeys'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429041941_VerificationKeys', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    ALTER TABLE [users] ADD [FailedLoginAttempts] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    ALTER TABLE [users] ADD [IsLockedOut] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    ALTER TABLE [users] ADD [LockedOutUntil] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    ALTER TABLE [student_profiles] ADD [GraduatedDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    ALTER TABLE [student_profiles] ADD [Status] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE TABLE [admin_change_requests] (
        [Id] uniqueidentifier NOT NULL,
        [RequestorUserId] uniqueidentifier NOT NULL,
        [ReviewedByUserId] uniqueidentifier NULL,
        [Status] int NOT NULL,
        [ChangeDescription] nvarchar(500) NOT NULL,
        [Reason] nvarchar(2000) NULL,
        [NewData] NVARCHAR(MAX) NOT NULL,
        [AdminNotes] nvarchar(2000) NULL,
        [ReviewedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_admin_change_requests] PRIMARY KEY ([Id]),
        CONSTRAINT [fk_acr_requestor_user] FOREIGN KEY ([RequestorUserId]) REFERENCES [users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_acr_reviewer_user] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE TABLE [payment_receipts] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [Amount] decimal(10,2) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [ProofOfPaymentPath] nvarchar(500) NULL,
        [ProofUploadedAt] datetime2 NULL,
        [ConfirmedByUserId] uniqueidentifier NULL,
        [ConfirmedAt] datetime2 NULL,
        [Notes] nvarchar(2000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_payment_receipts] PRIMARY KEY ([Id]),
        CONSTRAINT [fk_pr_confirmed_by_user] FOREIGN KEY ([ConfirmedByUserId]) REFERENCES [users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_pr_created_by_user] FOREIGN KEY ([CreatedByUserId]) REFERENCES [users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_pr_student_profile] FOREIGN KEY ([StudentProfileId]) REFERENCES [student_profiles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    CREATE TABLE [teacher_modification_requests] (
        [Id] uniqueidentifier NOT NULL,
        [TeacherUserId] uniqueidentifier NOT NULL,
        [ReviewedByUserId] uniqueidentifier NULL,
        [ModificationType] int NOT NULL,
        [RecordId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [ProposedData] NVARCHAR(MAX) NOT NULL,
        [AdminNotes] nvarchar(2000) NULL,
        [ReviewedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_teacher_modification_requests] PRIMARY KEY ([Id]),
        CONSTRAINT [fk_tmr_reviewer_user] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_tmr_teacher_user] FOREIGN KEY ([TeacherUserId]) REFERENCES [users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'ix_acr_requestor_status' AND [object_id] = OBJECT_ID(N'[admin_change_requests]'))
    BEGIN
        CREATE INDEX [ix_acr_requestor_status] ON [admin_change_requests] ([RequestorUserId], [Status]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'ix_acr_requestor_user_id' AND [object_id] = OBJECT_ID(N'[admin_change_requests]'))
    BEGIN
        CREATE INDEX [ix_acr_requestor_user_id] ON [admin_change_requests] ([RequestorUserId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'ix_acr_status' AND [object_id] = OBJECT_ID(N'[admin_change_requests]'))
    BEGIN
        CREATE INDEX [ix_acr_status] ON [admin_change_requests] ([Status]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_admin_change_requests_ReviewedByUserId' AND [object_id] = OBJECT_ID(N'[admin_change_requests]'))
    BEGIN
        CREATE INDEX [IX_admin_change_requests_ReviewedByUserId] ON [admin_change_requests] ([ReviewedByUserId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_payment_receipts_ConfirmedByUserId' AND [object_id] = OBJECT_ID(N'[payment_receipts]'))
    BEGIN
        CREATE INDEX [IX_payment_receipts_ConfirmedByUserId] ON [payment_receipts] ([ConfirmedByUserId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_payment_receipts_CreatedByUserId' AND [object_id] = OBJECT_ID(N'[payment_receipts]'))
    BEGIN
        CREATE INDEX [IX_payment_receipts_CreatedByUserId] ON [payment_receipts] ([CreatedByUserId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'ix_pr_due_date' AND [object_id] = OBJECT_ID(N'[payment_receipts]'))
    BEGIN
        CREATE INDEX [ix_pr_due_date] ON [payment_receipts] ([DueDate]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'ix_pr_status' AND [object_id] = OBJECT_ID(N'[payment_receipts]'))
    BEGIN
        CREATE INDEX [ix_pr_status] ON [payment_receipts] ([Status]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'ix_pr_student_profile_id' AND [object_id] = OBJECT_ID(N'[payment_receipts]'))
    BEGIN
        CREATE INDEX [ix_pr_student_profile_id] ON [payment_receipts] ([StudentProfileId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'ix_pr_student_status' AND [object_id] = OBJECT_ID(N'[payment_receipts]'))
    BEGIN
        CREATE INDEX [ix_pr_student_status] ON [payment_receipts] ([StudentProfileId], [Status]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_teacher_modification_requests_ReviewedByUserId' AND [object_id] = OBJECT_ID(N'[teacher_modification_requests]'))
    BEGIN
        CREATE INDEX [IX_teacher_modification_requests_ReviewedByUserId] ON [teacher_modification_requests] ([ReviewedByUserId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'ix_tmr_modification_type' AND [object_id] = OBJECT_ID(N'[teacher_modification_requests]'))
    BEGIN
        CREATE INDEX [ix_tmr_modification_type] ON [teacher_modification_requests] ([ModificationType]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'ix_tmr_record_id' AND [object_id] = OBJECT_ID(N'[teacher_modification_requests]'))
    BEGIN
        CREATE INDEX [ix_tmr_record_id] ON [teacher_modification_requests] ([RecordId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'ix_tmr_status' AND [object_id] = OBJECT_ID(N'[teacher_modification_requests]'))
    BEGIN
        CREATE INDEX [ix_tmr_status] ON [teacher_modification_requests] ([Status]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'ix_tmr_teacher_status' AND [object_id] = OBJECT_ID(N'[teacher_modification_requests]'))
    BEGIN
        CREATE INDEX [ix_tmr_teacher_status] ON [teacher_modification_requests] ([TeacherUserId], [Status]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'ix_tmr_teacher_user_id' AND [object_id] = OBJECT_ID(N'[teacher_modification_requests]'))
    BEGIN
        CREATE INDEX [ix_tmr_teacher_user_id] ON [teacher_modification_requests] ([TeacherUserId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429043652_StudentLifecycle'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429043652_StudentLifecycle', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429045706_AccountLockout'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[users]') AND [c].[name] = N'IsLockedOut');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [users] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [users] ADD DEFAULT CAST(0 AS bit) FOR [IsLockedOut];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429045706_AccountLockout'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[users]') AND [c].[name] = N'FailedLoginAttempts');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [users] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [users] ADD DEFAULT 0 FOR [FailedLoginAttempts];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429045706_AccountLockout'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_users_is_locked_out' AND [object_id] = OBJECT_ID(N'[users]'))
    BEGIN
        EXEC(N'CREATE INDEX [IX_users_is_locked_out] ON [users] ([IsLockedOut]) WHERE [IsLockedOut] = 1');
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429045706_AccountLockout'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429045706_AccountLockout', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    ALTER TABLE [users] ADD [ThemeKey] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    CREATE TABLE [module_role_assignments] (
        [Id] uniqueidentifier NOT NULL,
        [ModuleId] uniqueidentifier NOT NULL,
        [RoleName] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_module_role_assignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_module_role_assignments_modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [modules] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    CREATE TABLE [report_definitions] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Purpose] nvarchar(500) NOT NULL,
        [Key] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_report_definitions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    CREATE TABLE [timetables] (
        [Id] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [SemesterId] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [IsPublished] bit NOT NULL DEFAULT CAST(0 AS bit),
        [PublishedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_timetables] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_timetables_departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [departments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_timetables_semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [semesters] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    CREATE TABLE [report_role_assignments] (
        [Id] uniqueidentifier NOT NULL,
        [ReportDefinitionId] uniqueidentifier NOT NULL,
        [RoleName] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_report_role_assignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_report_role_assignments_report_definitions_ReportDefinitionId] FOREIGN KEY ([ReportDefinitionId]) REFERENCES [report_definitions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    CREATE TABLE [timetable_entries] (
        [Id] uniqueidentifier NOT NULL,
        [TimetableId] uniqueidentifier NOT NULL,
        [DayOfWeek] int NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [SubjectName] nvarchar(200) NOT NULL,
        [RoomNumber] nvarchar(50) NULL,
        [FacultyName] nvarchar(200) NULL,
        [CourseOfferingId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_timetable_entries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_timetable_entries_timetables_TimetableId] FOREIGN KEY ([TimetableId]) REFERENCES [timetables] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_module_role_assignments_unique' AND [object_id] = OBJECT_ID(N'[module_role_assignments]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_module_role_assignments_unique] ON [module_role_assignments] ([ModuleId], [RoleName]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_report_definitions_key' AND [object_id] = OBJECT_ID(N'[report_definitions]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_report_definitions_key] ON [report_definitions] ([Key]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_report_role_assignments_unique' AND [object_id] = OBJECT_ID(N'[report_role_assignments]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_report_role_assignments_unique] ON [report_role_assignments] ([ReportDefinitionId], [RoleName]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_timetable_entries_timetable_day' AND [object_id] = OBJECT_ID(N'[timetable_entries]'))
    BEGIN
        CREATE INDEX [IX_timetable_entries_timetable_day] ON [timetable_entries] ([TimetableId], [DayOfWeek]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_timetables_dept_semester' AND [object_id] = OBJECT_ID(N'[timetables]'))
    BEGIN
        CREATE INDEX [IX_timetables_dept_semester] ON [timetables] ([DepartmentId], [SemesterId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_timetables_SemesterId' AND [object_id] = OBJECT_ID(N'[timetables]'))
    BEGIN
        CREATE INDEX [IX_timetables_SemesterId] ON [timetables] ([SemesterId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223425_Phase9DashboardSettings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429223425_Phase9DashboardSettings', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    DROP INDEX [IX_timetables_dept_semester] ON [timetables];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[timetables]') AND [c].[name] = N'Title');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [timetables] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [timetables] DROP COLUMN [Title];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    EXEC sp_rename N'[timetable_entries].[CourseOfferingId]', N'RoomId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetables] ADD [AcademicProgramId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetables] ADD [EffectiveDate] date NOT NULL DEFAULT '0001-01-01';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetables] ADD [SemesterNumber] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetable_entries] ADD [BuildingId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetable_entries] ADD [CourseId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetable_entries] ADD [FacultyUserId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    CREATE TABLE [buildings] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [CampusId] uniqueidentifier NULL,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_buildings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    CREATE TABLE [rooms] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [CampusId] uniqueidentifier NULL,
        [Number] nvarchar(50) NOT NULL,
        [BuildingId] uniqueidentifier NOT NULL,
        [Capacity] int NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_rooms] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_rooms_buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [buildings] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_timetables_AcademicProgramId' AND [object_id] = OBJECT_ID(N'[timetables]'))
    BEGIN
        CREATE INDEX [IX_timetables_AcademicProgramId] ON [timetables] ([AcademicProgramId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_timetables_dept_program_semester' AND [object_id] = OBJECT_ID(N'[timetables]'))
    BEGIN
        CREATE INDEX [IX_timetables_dept_program_semester] ON [timetables] ([DepartmentId], [AcademicProgramId], [SemesterId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_timetable_entries_BuildingId' AND [object_id] = OBJECT_ID(N'[timetable_entries]'))
    BEGIN
        CREATE INDEX [IX_timetable_entries_BuildingId] ON [timetable_entries] ([BuildingId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_timetable_entries_CourseId' AND [object_id] = OBJECT_ID(N'[timetable_entries]'))
    BEGIN
        CREATE INDEX [IX_timetable_entries_CourseId] ON [timetable_entries] ([CourseId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_timetable_entries_faculty_user' AND [object_id] = OBJECT_ID(N'[timetable_entries]'))
    BEGIN
        CREATE INDEX [IX_timetable_entries_faculty_user] ON [timetable_entries] ([FacultyUserId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_timetable_entries_RoomId' AND [object_id] = OBJECT_ID(N'[timetable_entries]'))
    BEGIN
        CREATE INDEX [IX_timetable_entries_RoomId] ON [timetable_entries] ([RoomId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_buildings_scope_code' AND [object_id] = OBJECT_ID(N'[buildings]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_buildings_scope_code] ON [buildings] ([TenantId], [CampusId], [Code]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_rooms_scope_building_number' AND [object_id] = OBJECT_ID(N'[rooms]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_rooms_scope_building_number] ON [rooms] ([TenantId], [CampusId], [BuildingId], [Number])
            WHERE [TenantId] IS NOT NULL AND [CampusId] IS NOT NULL;
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetable_entries] ADD CONSTRAINT [FK_timetable_entries_buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [buildings] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetable_entries] ADD CONSTRAINT [FK_timetable_entries_courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [courses] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetable_entries] ADD CONSTRAINT [FK_timetable_entries_rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [rooms] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetable_entries] ADD CONSTRAINT [FK_timetable_entries_users_FacultyUserId] FOREIGN KEY ([FacultyUserId]) REFERENCES [users] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    ALTER TABLE [timetables] ADD CONSTRAINT [FK_timetables_academic_programs_AcademicProgramId] FOREIGN KEY ([AcademicProgramId]) REFERENCES [academic_programs] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429230253_Phase9TimetableRedesign'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429230253_Phase9TimetableRedesign', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430000234_Phase9SidebarSettings'
)
BEGIN
    CREATE TABLE [sidebar_menu_items] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Purpose] nvarchar(500) NOT NULL,
        [Key] nvarchar(100) NOT NULL,
        [ParentId] uniqueidentifier NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [IsSystemMenu] bit NOT NULL DEFAULT CAST(0 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_sidebar_menu_items] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_sidebar_menu_items_sidebar_menu_items_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [sidebar_menu_items] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430000234_Phase9SidebarSettings'
)
BEGIN
    CREATE TABLE [sidebar_menu_role_accesses] (
        [Id] uniqueidentifier NOT NULL,
        [SidebarMenuItemId] uniqueidentifier NOT NULL,
        [RoleName] nvarchar(100) NOT NULL,
        [IsAllowed] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_sidebar_menu_role_accesses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_sidebar_menu_role_accesses_sidebar_menu_items_SidebarMenuItemId] FOREIGN KEY ([SidebarMenuItemId]) REFERENCES [sidebar_menu_items] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430000234_Phase9SidebarSettings'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_sidebar_menu_items_key' AND [object_id] = OBJECT_ID(N'[sidebar_menu_items]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_sidebar_menu_items_key] ON [sidebar_menu_items] ([Key]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430000234_Phase9SidebarSettings'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_sidebar_menu_items_ParentId' AND [object_id] = OBJECT_ID(N'[sidebar_menu_items]'))
    BEGIN
        CREATE INDEX [IX_sidebar_menu_items_ParentId] ON [sidebar_menu_items] ([ParentId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430000234_Phase9SidebarSettings'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_sidebar_menu_role_accesses_item_role' AND [object_id] = OBJECT_ID(N'[sidebar_menu_role_accesses]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_sidebar_menu_role_accesses_item_role] ON [sidebar_menu_role_accesses] ([SidebarMenuItemId], [RoleName]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430000234_Phase9SidebarSettings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260430000234_Phase9SidebarSettings', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430045628_Phase10PerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_audit_logs_entity_occurred_at' AND [object_id] = OBJECT_ID(N'[audit_logs]'))
    BEGIN
        CREATE INDEX [IX_audit_logs_entity_occurred_at] ON [audit_logs] ([EntityName], [OccurredAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430045628_Phase10PerformanceIndexes'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_assignments_offering_published' AND [object_id] = OBJECT_ID(N'[assignments]'))
    BEGIN
        CREATE INDEX [IX_assignments_offering_published] ON [assignments] ([CourseOfferingId], [IsPublished]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430045628_Phase10PerformanceIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260430045628_Phase10PerformanceIndexes', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430141918_Phase10SecurityTables'
)
BEGIN
    CREATE TABLE [outbound_email_logs] (
        [Id] uniqueidentifier NOT NULL,
        [ToAddress] nvarchar(256) NOT NULL,
        [Subject] nvarchar(500) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [ErrorMessage] nvarchar(2000) NULL,
        [AttemptedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_outbound_email_logs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430141918_Phase10SecurityTables'
)
BEGIN
    CREATE TABLE [password_history] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [PasswordHash] nvarchar(512) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_password_history] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430141918_Phase10SecurityTables'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_outbound_email_logs_status_attempted' AND [object_id] = OBJECT_ID(N'[outbound_email_logs]'))
    BEGIN
        CREATE INDEX [IX_outbound_email_logs_status_attempted] ON [outbound_email_logs] ([Status], [AttemptedAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430141918_Phase10SecurityTables'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_password_history_user_created' AND [object_id] = OBJECT_ID(N'[password_history]'))
    BEGIN
        CREATE INDEX [IX_password_history_user_created] ON [password_history] ([UserId], [CreatedAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430141918_Phase10SecurityTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260430141918_Phase10SecurityTables', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430142338_Phase10StoredProcedures'
)
BEGIN
    IF OBJECT_ID(N'dbo.sp_get_attendance_below_threshold', N'P') IS NULL
        EXEC(N'CREATE PROCEDURE dbo.sp_get_attendance_below_threshold AS BEGIN SET NOCOUNT ON; END;');

    EXEC(N'
    ALTER PROCEDURE dbo.sp_get_attendance_below_threshold
        @ThresholdPercent DECIMAL(5,2) = 75.0,
        @CourseOfferingId UNIQUEIDENTIFIER = NULL
    AS
    BEGIN
        SET NOCOUNT ON;

        SELECT
            ar.StudentProfileId,
            ar.CourseOfferingId,
            COUNT(*) AS TotalSessions,
            SUM(CASE WHEN ar.Status = ''Present'' THEN 1 ELSE 0 END) AS AttendedSessions,
            CAST(
                CASE WHEN COUNT(*) = 0 THEN 0.0
                     ELSE (SUM(CASE WHEN ar.Status = ''Present'' THEN 1.0 ELSE 0.0 END) / COUNT(*)) * 100.0
                END AS DECIMAL(5,2)
            ) AS AttendancePercentage
        FROM attendance_records ar
        WHERE (@CourseOfferingId IS NULL OR ar.CourseOfferingId = @CourseOfferingId)
        GROUP BY ar.StudentProfileId, ar.CourseOfferingId
        HAVING
            CASE WHEN COUNT(*) = 0 THEN 0.0
                 ELSE (SUM(CASE WHEN ar.Status = ''Present'' THEN 1.0 ELSE 0.0 END) / COUNT(*)) * 100.0
            END < @ThresholdPercent;
    END;');

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430142338_Phase10StoredProcedures'
)
BEGIN
    IF OBJECT_ID(N'dbo.sp_recalculate_student_cgpa', N'P') IS NULL
        EXEC(N'CREATE PROCEDURE dbo.sp_recalculate_student_cgpa AS BEGIN SET NOCOUNT ON; END;');

    EXEC(N'
    ALTER PROCEDURE dbo.sp_recalculate_student_cgpa
        @StudentProfileId UNIQUEIDENTIFIER
    AS
    BEGIN
        SET NOCOUNT ON;

        DECLARE @TotalWeightedMarks DECIMAL(18,4) = 0;
        DECLARE @TotalMaxMarks DECIMAL(18,4) = 0;
        DECLARE @NewCgpa DECIMAL(4,2) = 0;

        SELECT
            @TotalWeightedMarks = SUM(CAST(r.MarksObtained AS DECIMAL(18,4))),
            @TotalMaxMarks = SUM(CAST(r.MaxMarks AS DECIMAL(18,4)))
        FROM results r
        WHERE r.StudentProfileId = @StudentProfileId
          AND r.IsPublished = 1
          AND r.MaxMarks > 0;

        IF @TotalMaxMarks > 0
        BEGIN
            -- Convert percentage to 4.0 GPA scale (proportional mapping: 100% -> 4.0)
            SET @NewCgpa = CAST((@TotalWeightedMarks / @TotalMaxMarks) * 4.0 AS DECIMAL(4,2));
            IF @NewCgpa > 4.0 SET @NewCgpa = 4.0;
        END

        UPDATE student_profiles
        SET Cgpa = @NewCgpa
        WHERE Id = @StudentProfileId;

        SELECT @NewCgpa AS NewCgpa;
    END;');

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430142338_Phase10StoredProcedures'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260430142338_Phase10StoredProcedures', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430143000_Phase10SqlViews'
)
BEGIN
    IF OBJECT_ID(N'dbo.vw_student_attendance_summary', N'V') IS NULL
        EXEC(N'CREATE VIEW dbo.vw_student_attendance_summary AS SELECT 1 AS Placeholder;');

    EXEC(N'
    ALTER VIEW dbo.vw_student_attendance_summary AS
    SELECT
        ar.StudentProfileId,
        ar.CourseOfferingId,
        COUNT(*) AS TotalSessions,
        SUM(CASE WHEN ar.Status = ''Present'' THEN 1 ELSE 0 END) AS AttendedSessions,
        CAST(
            CASE WHEN COUNT(*) = 0 THEN 0.0
                 ELSE (SUM(CASE WHEN ar.Status = ''Present'' THEN 1.0 ELSE 0.0 END) / COUNT(*)) * 100.0
            END AS decimal(5,2)
        ) AS AttendancePercentage
    FROM attendance_records ar
    GROUP BY ar.StudentProfileId, ar.CourseOfferingId;');

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430143000_Phase10SqlViews'
)
BEGIN
    IF OBJECT_ID(N'dbo.vw_student_results_summary', N'V') IS NULL
        EXEC(N'CREATE VIEW dbo.vw_student_results_summary AS SELECT 1 AS Placeholder;');

    EXEC(N'
    ALTER VIEW dbo.vw_student_results_summary AS
    SELECT
        r.StudentProfileId,
        r.CourseOfferingId,
        r.ResultType,
        r.MarksObtained,
        r.MaxMarks,
        CAST(
            CASE WHEN r.MaxMarks = 0 THEN 0.0
                 ELSE (CAST(r.MarksObtained AS decimal(10,2)) / r.MaxMarks) * 100.0
            END AS decimal(5,2)
        ) AS Percentage,
        r.PublishedAt,
        co.CourseId,
        c.Code AS CourseCode,
        c.Title AS CourseTitle,
        co.SemesterId
    FROM results r
    INNER JOIN course_offerings co ON co.Id = r.CourseOfferingId
    INNER JOIN courses c ON c.Id = co.CourseId
    WHERE r.IsPublished = 1;');

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430143000_Phase10SqlViews'
)
BEGIN
    IF OBJECT_ID(N'dbo.vw_course_enrollment_summary', N'V') IS NULL
        EXEC(N'CREATE VIEW dbo.vw_course_enrollment_summary AS SELECT 1 AS Placeholder;');

    EXEC(N'
    ALTER VIEW dbo.vw_course_enrollment_summary AS
    SELECT
        co.Id AS CourseOfferingId,
        co.CourseId,
        c.Code AS CourseCode,
        c.Title AS CourseTitle,
        co.SemesterId,
        co.MaxEnrollment,
        COUNT(e.Id) AS EnrolledCount,
        co.MaxEnrollment - COUNT(e.Id) AS AvailableSeats
    FROM course_offerings co
    INNER JOIN courses c ON c.Id = co.CourseId
    LEFT JOIN enrollments e ON e.CourseOfferingId = co.Id AND e.Status = ''Active''
    WHERE co.IsOpen = 1
    GROUP BY co.Id, co.CourseId, c.Code, c.Title, co.SemesterId, co.MaxEnrollment;');

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430143000_Phase10SqlViews'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260430143000_Phase10SqlViews', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502134611_Phase11ResultCalculation'
)
BEGIN
    ALTER TABLE [student_profiles] ADD [CurrentSemesterGpa] decimal(4,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502134611_Phase11ResultCalculation'
)
BEGIN
    DROP INDEX [IX_results_student_offering_type] ON [results];
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[results]') AND [c].[name] = N'ResultType');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [results] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [results] ALTER COLUMN [ResultType] nvarchar(100) NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_results_student_offering_type' AND [object_id] = OBJECT_ID(N'[results]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_results_student_offering_type] ON [results] ([StudentProfileId], [CourseOfferingId], [ResultType]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502134611_Phase11ResultCalculation'
)
BEGIN
    ALTER TABLE [results] ADD [GradePoint] decimal(4,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502134611_Phase11ResultCalculation'
)
BEGIN
    CREATE TABLE [gpa_scale_rules] (
        [Id] uniqueidentifier NOT NULL,
        [InstitutionType] int NOT NULL CONSTRAINT [DF_gpa_scale_rules_InstitutionType] DEFAULT(0),
        [GradePoint] decimal(4,2) NOT NULL,
        [MinimumScore] decimal(5,2) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_gpa_scale_rules] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502134611_Phase11ResultCalculation'
)
BEGIN
    CREATE TABLE [result_component_rules] (
        [Id] uniqueidentifier NOT NULL,
        [InstitutionType] int NOT NULL CONSTRAINT [DF_result_component_rules_InstitutionType] DEFAULT(0),
        [Name] nvarchar(100) NOT NULL,
        [Weightage] decimal(5,2) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_result_component_rules] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502134611_Phase11ResultCalculation'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_gpa_scale_rules_institution_minimum_score' AND [object_id] = OBJECT_ID(N'[gpa_scale_rules]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_gpa_scale_rules_institution_minimum_score] ON [gpa_scale_rules] ([InstitutionType], [MinimumScore]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502134611_Phase11ResultCalculation'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_result_component_rules_institution_name' AND [object_id] = OBJECT_ID(N'[result_component_rules]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_result_component_rules_institution_name] ON [result_component_rules] ([InstitutionType], [Name]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526010000_ResultCalculationInstitutionScope'
)
BEGIN
    IF OBJECT_ID(N'[gpa_scale_rules]') IS NOT NULL
    BEGIN
        IF COL_LENGTH(N'gpa_scale_rules', N'InstitutionType') IS NULL
        BEGIN
            ALTER TABLE [gpa_scale_rules]
            ADD [InstitutionType] int NOT NULL CONSTRAINT [DF_gpa_scale_rules_InstitutionType] DEFAULT(0);
        END;

        IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_gpa_scale_rules_minimum_score' AND [object_id] = OBJECT_ID(N'[gpa_scale_rules]'))
            DROP INDEX [IX_gpa_scale_rules_minimum_score] ON [gpa_scale_rules];

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_gpa_scale_rules_institution_minimum_score' AND [object_id] = OBJECT_ID(N'[gpa_scale_rules]'))
            CREATE UNIQUE INDEX [IX_gpa_scale_rules_institution_minimum_score] ON [gpa_scale_rules] ([InstitutionType], [MinimumScore]);
    END;

    IF OBJECT_ID(N'[result_component_rules]') IS NOT NULL
    BEGIN
        IF COL_LENGTH(N'result_component_rules', N'InstitutionType') IS NULL
        BEGIN
            ALTER TABLE [result_component_rules]
            ADD [InstitutionType] int NOT NULL CONSTRAINT [DF_result_component_rules_InstitutionType] DEFAULT(0);
        END;

        IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_result_component_rules_name' AND [object_id] = OBJECT_ID(N'[result_component_rules]'))
            DROP INDEX [IX_result_component_rules_name] ON [result_component_rules];

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_result_component_rules_institution_name' AND [object_id] = OBJECT_ID(N'[result_component_rules]'))
            CREATE UNIQUE INDEX [IX_result_component_rules_institution_name] ON [result_component_rules] ([InstitutionType], [Name]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526010000_ResultCalculationInstitutionScope'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260526010000_ResultCalculationInstitutionScope', N'8.0.8');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502134611_Phase11ResultCalculation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260502134611_Phase11ResultCalculation', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503210356_Phase1DashboardBranding'
)
BEGIN
    CREATE TABLE [portal_settings] (
        [Id] uniqueidentifier NOT NULL,
        [Key] nvarchar(100) NOT NULL,
        [Value] nvarchar(1000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_portal_settings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503210356_Phase1DashboardBranding'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_portal_settings_key' AND [object_id] = OBJECT_ID(N'[portal_settings]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_portal_settings_key] ON [portal_settings] ([Key]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503210356_Phase1DashboardBranding'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260503210356_Phase1DashboardBranding', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505_Phase2LicenseConcurrency'
)
BEGIN
    ALTER TABLE [license_state] ADD [ActivatedDomain] nvarchar(253) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505_Phase2LicenseConcurrency'
)
BEGIN
    ALTER TABLE [license_state] ADD [MaxUsers] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505_Phase2LicenseConcurrency'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260505_Phase2LicenseConcurrency', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506_Phase4UserImport'
)
BEGIN
    ALTER TABLE [users] ADD [MustChangePassword] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506_Phase4UserImport'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506_Phase4UserImport', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506035554_Phase4FypCompletionApprovalFlow'
)
BEGIN

    IF COL_LENGTH('license_state', 'ActivatedDomain') IS NULL
    BEGIN
        ALTER TABLE [license_state] ADD [ActivatedDomain] nvarchar(253) NULL;
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506035554_Phase4FypCompletionApprovalFlow'
)
BEGIN

    IF COL_LENGTH('license_state', 'MaxUsers') IS NULL
    BEGIN
        ALTER TABLE [license_state] ADD [MaxUsers] int NOT NULL CONSTRAINT [DF_license_state_MaxUsers] DEFAULT (0);
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506035554_Phase4FypCompletionApprovalFlow'
)
BEGIN
    ALTER TABLE [fyp_projects] ADD [CompletionApprovedByUserIdsCsv] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506035554_Phase4FypCompletionApprovalFlow'
)
BEGIN
    ALTER TABLE [fyp_projects] ADD [CompletionRequestedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506035554_Phase4FypCompletionApprovalFlow'
)
BEGIN
    ALTER TABLE [fyp_projects] ADD [CompletionRequestedByStudentProfileId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506035554_Phase4FypCompletionApprovalFlow'
)
BEGIN
    ALTER TABLE [fyp_projects] ADD [IsCompletionRequested] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506035554_Phase4FypCompletionApprovalFlow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506035554_Phase4FypCompletionApprovalFlow', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506044806_20260506_Phase6AdminDepartmentAssignments'
)
BEGIN
    CREATE TABLE [admin_department_assignments] (
        [Id] uniqueidentifier NOT NULL,
        [AdminUserId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        [RemovedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_admin_department_assignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_admin_department_assignments_departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [departments] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506044806_20260506_Phase6AdminDepartmentAssignments'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_admin_department_assignments_DepartmentId' AND [object_id] = OBJECT_ID(N'[admin_department_assignments]'))
    BEGIN
        CREATE INDEX [IX_admin_department_assignments_DepartmentId] ON [admin_department_assignments] ([DepartmentId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506044806_20260506_Phase6AdminDepartmentAssignments'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_admin_dept_assignments_admin_dept' AND [object_id] = OBJECT_ID(N'[admin_department_assignments]'))
    BEGIN
        CREATE INDEX [IX_admin_dept_assignments_admin_dept] ON [admin_department_assignments] ([AdminUserId], [DepartmentId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506044806_20260506_Phase6AdminDepartmentAssignments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506044806_20260506_Phase6AdminDepartmentAssignments', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507044625_20260507_Phase12AcademicCalendar'
)
BEGIN
    CREATE TABLE [academic_deadlines] (
        [Id] uniqueidentifier NOT NULL,
        [SemesterId] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [DeadlineDate] datetime2 NOT NULL,
        [ReminderDaysBefore] int NOT NULL,
        [IsActive] bit NOT NULL,
        [LastReminderSentAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_academic_deadlines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_academic_deadlines_semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [semesters] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507044625_20260507_Phase12AcademicCalendar'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_academic_deadlines_date_active' AND [object_id] = OBJECT_ID(N'[academic_deadlines]'))
    BEGIN
        CREATE INDEX [IX_academic_deadlines_date_active] ON [academic_deadlines] ([DeadlineDate], [IsActive]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507044625_20260507_Phase12AcademicCalendar'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_academic_deadlines_semester' AND [object_id] = OBJECT_ID(N'[academic_deadlines]'))
    BEGIN
        CREATE INDEX [IX_academic_deadlines_semester] ON [academic_deadlines] ([SemesterId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507044625_20260507_Phase12AcademicCalendar'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507044625_20260507_Phase12AcademicCalendar', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507055009_Phase14_Helpdesk'
)
BEGIN
    CREATE TABLE [support_tickets] (
        [Id] uniqueidentifier NOT NULL,
        [SubmitterId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NULL,
        [Category] int NOT NULL,
        [Subject] nvarchar(300) NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [Status] int NOT NULL,
        [AssignedToId] uniqueidentifier NULL,
        [ResolvedAt] datetime2 NULL,
        [ReopenWindowDays] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_support_tickets] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507055009_Phase14_Helpdesk'
)
BEGIN
    CREATE TABLE [support_ticket_messages] (
        [Id] uniqueidentifier NOT NULL,
        [TicketId] uniqueidentifier NOT NULL,
        [AuthorId] uniqueidentifier NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [IsInternalNote] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_support_ticket_messages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_support_ticket_messages_support_tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [support_tickets] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507055009_Phase14_Helpdesk'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_support_ticket_messages_author' AND [object_id] = OBJECT_ID(N'[support_ticket_messages]'))
    BEGIN
        CREATE INDEX [IX_support_ticket_messages_author] ON [support_ticket_messages] ([AuthorId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507055009_Phase14_Helpdesk'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_support_ticket_messages_ticket' AND [object_id] = OBJECT_ID(N'[support_ticket_messages]'))
    BEGIN
        CREATE INDEX [IX_support_ticket_messages_ticket] ON [support_ticket_messages] ([TicketId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507055009_Phase14_Helpdesk'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_support_tickets_assigned' AND [object_id] = OBJECT_ID(N'[support_tickets]'))
    BEGIN
        CREATE INDEX [IX_support_tickets_assigned] ON [support_tickets] ([AssignedToId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507055009_Phase14_Helpdesk'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_support_tickets_department' AND [object_id] = OBJECT_ID(N'[support_tickets]'))
    BEGIN
        CREATE INDEX [IX_support_tickets_department] ON [support_tickets] ([DepartmentId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507055009_Phase14_Helpdesk'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_support_tickets_status' AND [object_id] = OBJECT_ID(N'[support_tickets]'))
    BEGIN
        CREATE INDEX [IX_support_tickets_status] ON [support_tickets] ([Status]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507055009_Phase14_Helpdesk'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_support_tickets_submitter' AND [object_id] = OBJECT_ID(N'[support_tickets]'))
    BEGIN
        CREATE INDEX [IX_support_tickets_submitter] ON [support_tickets] ([SubmitterId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507055009_Phase14_Helpdesk'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507055009_Phase14_Helpdesk', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507103000_PortalBrandingLogoValueMaxLength'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[portal_settings]') AND [c].[name] = N'Value');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [portal_settings] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [portal_settings] ALTER COLUMN [Value] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507103000_PortalBrandingLogoValueMaxLength'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507103000_PortalBrandingLogoValueMaxLength', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507133254_Phase15_EnrollmentRules'
)
BEGIN
    CREATE TABLE [course_prerequisites] (
        [Id] uniqueidentifier NOT NULL,
        [CourseId] uniqueidentifier NOT NULL,
        [PrerequisiteCourseId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_course_prerequisites] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_course_prerequisites_courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [courses] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_course_prerequisites_courses_PrerequisiteCourseId] FOREIGN KEY ([PrerequisiteCourseId]) REFERENCES [courses] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507133254_Phase15_EnrollmentRules'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_course_prerequisites_course_prereq' AND [object_id] = OBJECT_ID(N'[course_prerequisites]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_course_prerequisites_course_prereq] ON [course_prerequisites] ([CourseId], [PrerequisiteCourseId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507133254_Phase15_EnrollmentRules'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_course_prerequisites_PrerequisiteCourseId' AND [object_id] = OBJECT_ID(N'[course_prerequisites]'))
    BEGIN
        CREATE INDEX [IX_course_prerequisites_PrerequisiteCourseId] ON [course_prerequisites] ([PrerequisiteCourseId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507133254_Phase15_EnrollmentRules'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507133254_Phase15_EnrollmentRules', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    CREATE TABLE [rubric_student_grades] (
        [Id] uniqueidentifier NOT NULL,
        [AssignmentSubmissionId] uniqueidentifier NOT NULL,
        [RubricCriterionId] uniqueidentifier NOT NULL,
        [RubricLevelId] uniqueidentifier NOT NULL,
        [PointsAwarded] decimal(8,2) NOT NULL,
        [GradedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_rubric_student_grades] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    CREATE TABLE [rubrics] (
        [Id] uniqueidentifier NOT NULL,
        [AssignmentId] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_rubrics] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    CREATE TABLE [rubric_criteria] (
        [Id] uniqueidentifier NOT NULL,
        [RubricId] uniqueidentifier NOT NULL,
        [Name] nvarchar(300) NOT NULL,
        [MaxPoints] decimal(8,2) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_rubric_criteria] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_rubric_criteria_rubrics_RubricId] FOREIGN KEY ([RubricId]) REFERENCES [rubrics] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    CREATE TABLE [rubric_levels] (
        [Id] uniqueidentifier NOT NULL,
        [CriterionId] uniqueidentifier NOT NULL,
        [Label] nvarchar(200) NOT NULL,
        [PointsAwarded] decimal(8,2) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [RubricCriterionId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_rubric_levels] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_rubric_levels_rubric_criteria_RubricCriterionId] FOREIGN KEY ([RubricCriterionId]) REFERENCES [rubric_criteria] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_rubric_criteria_rubric_id' AND [object_id] = OBJECT_ID(N'[rubric_criteria]'))
    BEGIN
        CREATE INDEX [IX_rubric_criteria_rubric_id] ON [rubric_criteria] ([RubricId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_rubric_levels_criterion_id' AND [object_id] = OBJECT_ID(N'[rubric_levels]'))
    BEGIN
        CREATE INDEX [IX_rubric_levels_criterion_id] ON [rubric_levels] ([CriterionId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_rubric_levels_RubricCriterionId' AND [object_id] = OBJECT_ID(N'[rubric_levels]'))
    BEGIN
        CREATE INDEX [IX_rubric_levels_RubricCriterionId] ON [rubric_levels] ([RubricCriterionId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_rubric_student_grades_submission_criterion' AND [object_id] = OBJECT_ID(N'[rubric_student_grades]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_rubric_student_grades_submission_criterion] ON [rubric_student_grades] ([AssignmentSubmissionId], [RubricCriterionId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_rubric_student_grades_submission_id' AND [object_id] = OBJECT_ID(N'[rubric_student_grades]'))
    BEGIN
        CREATE INDEX [IX_rubric_student_grades_submission_id] ON [rubric_student_grades] ([AssignmentSubmissionId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_rubrics_assignment_active' AND [object_id] = OBJECT_ID(N'[rubrics]'))
    BEGIN
        CREATE INDEX [IX_rubrics_assignment_active] ON [rubrics] ([AssignmentId], [IsActive]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507223356_Phase16_FacultyGrading'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507223356_Phase16_FacultyGrading', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507231326_Phase17_DegreeAudit'
)
BEGIN
    ALTER TABLE [courses] ADD [CourseType] int NOT NULL DEFAULT 1;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507231326_Phase17_DegreeAudit'
)
BEGIN
    CREATE TABLE [degree_rules] (
        [Id] uniqueidentifier NOT NULL,
        [AcademicProgramId] uniqueidentifier NOT NULL,
        [MinTotalCredits] int NOT NULL,
        [MinCoreCredits] int NOT NULL,
        [MinElectiveCredits] int NOT NULL,
        [MinGpa] decimal(4,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_degree_rules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_degree_rules_academic_programs_AcademicProgramId] FOREIGN KEY ([AcademicProgramId]) REFERENCES [academic_programs] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507231326_Phase17_DegreeAudit'
)
BEGIN
    CREATE TABLE [degree_rule_required_courses] (
        [Id] uniqueidentifier NOT NULL,
        [DegreeRuleId] uniqueidentifier NOT NULL,
        [CourseId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_degree_rule_required_courses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_degree_rule_required_courses_courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [courses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_degree_rule_required_courses_degree_rules_DegreeRuleId] FOREIGN KEY ([DegreeRuleId]) REFERENCES [degree_rules] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507231326_Phase17_DegreeAudit'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_degree_rule_required_courses_CourseId' AND [object_id] = OBJECT_ID(N'[degree_rule_required_courses]'))
    BEGIN
        CREATE INDEX [IX_degree_rule_required_courses_CourseId] ON [degree_rule_required_courses] ([CourseId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507231326_Phase17_DegreeAudit'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_degree_rule_required_courses_rule_course' AND [object_id] = OBJECT_ID(N'[degree_rule_required_courses]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_degree_rule_required_courses_rule_course] ON [degree_rule_required_courses] ([DegreeRuleId], [CourseId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507231326_Phase17_DegreeAudit'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_degree_rules_program' AND [object_id] = OBJECT_ID(N'[degree_rules]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_degree_rules_program] ON [degree_rules] ([AcademicProgramId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507231326_Phase17_DegreeAudit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507231326_Phase17_DegreeAudit', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508003259_Phase18_GraduationWorkflow'
)
BEGIN
    CREATE TABLE [graduation_applications] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [StudentNote] nvarchar(2000) NULL,
        [SubmittedAt] datetime2 NULL,
        [CertificatePath] nvarchar(500) NULL,
        [CertificateGeneratedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_graduation_applications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_graduation_applications_student_profiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [student_profiles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508003259_Phase18_GraduationWorkflow'
)
BEGIN
    CREATE TABLE [graduation_application_approvals] (
        [Id] uniqueidentifier NOT NULL,
        [GraduationApplicationId] uniqueidentifier NOT NULL,
        [Stage] int NOT NULL,
        [ApproverUserId] uniqueidentifier NOT NULL,
        [IsApproved] bit NOT NULL,
        [Note] nvarchar(1000) NULL,
        [ActedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_graduation_application_approvals] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_graduation_application_approvals_graduation_applications_GraduationApplicationId] FOREIGN KEY ([GraduationApplicationId]) REFERENCES [graduation_applications] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508003259_Phase18_GraduationWorkflow'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_graduation_application_approvals_GraduationApplicationId' AND [object_id] = OBJECT_ID(N'[graduation_application_approvals]'))
    BEGIN
        CREATE INDEX [IX_graduation_application_approvals_GraduationApplicationId] ON [graduation_application_approvals] ([GraduationApplicationId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508003259_Phase18_GraduationWorkflow'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_graduation_applications_StudentProfileId' AND [object_id] = OBJECT_ID(N'[graduation_applications]'))
    BEGIN
        CREATE INDEX [IX_graduation_applications_StudentProfileId] ON [graduation_applications] ([StudentProfileId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508003259_Phase18_GraduationWorkflow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260508003259_Phase18_GraduationWorkflow', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508043559_Phase19_CourseTypeAndGrading'
)
BEGIN
    ALTER TABLE [courses] ADD [DurationUnit] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508043559_Phase19_CourseTypeAndGrading'
)
BEGIN
    ALTER TABLE [courses] ADD [DurationValue] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508043559_Phase19_CourseTypeAndGrading'
)
BEGIN
    ALTER TABLE [courses] ADD [GradingType] nvarchar(20) NOT NULL DEFAULT N'GPA';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508043559_Phase19_CourseTypeAndGrading'
)
BEGIN
    ALTER TABLE [courses] ADD [HasSemesters] bit NOT NULL DEFAULT CAST(1 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508043559_Phase19_CourseTypeAndGrading'
)
BEGIN
    ALTER TABLE [courses] ADD [TotalSemesters] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508043559_Phase19_CourseTypeAndGrading'
)
BEGIN
    CREATE TABLE [course_grading_configs] (
        [Id] uniqueidentifier NOT NULL,
        [CourseId] uniqueidentifier NOT NULL,
        [PassThreshold] decimal(5,2) NOT NULL,
        [GradingType] nvarchar(20) NOT NULL,
        [GradeRangesJson] nvarchar(4000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_course_grading_configs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_course_grading_configs_courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [courses] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508043559_Phase19_CourseTypeAndGrading'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_course_grading_configs_courseId' AND [object_id] = OBJECT_ID(N'[course_grading_configs]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_course_grading_configs_courseId] ON [course_grading_configs] ([CourseId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508043559_Phase19_CourseTypeAndGrading'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260508043559_Phase19_CourseTypeAndGrading', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    CREATE TABLE [course_announcements] (
        [Id] uniqueidentifier NOT NULL,
        [OfferingId] uniqueidentifier NULL,
        [AuthorId] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [PostedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_course_announcements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_course_announcements_course_offerings_OfferingId] FOREIGN KEY ([OfferingId]) REFERENCES [course_offerings] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    CREATE TABLE [course_content_modules] (
        [Id] uniqueidentifier NOT NULL,
        [OfferingId] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [WeekNumber] int NOT NULL,
        [Body] nvarchar(max) NULL,
        [IsPublished] bit NOT NULL,
        [PublishedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_course_content_modules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_course_content_modules_course_offerings_OfferingId] FOREIGN KEY ([OfferingId]) REFERENCES [course_offerings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    CREATE TABLE [discussion_threads] (
        [Id] uniqueidentifier NOT NULL,
        [OfferingId] uniqueidentifier NOT NULL,
        [Title] nvarchar(500) NOT NULL,
        [AuthorId] uniqueidentifier NOT NULL,
        [IsPinned] bit NOT NULL,
        [IsClosed] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_discussion_threads] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_discussion_threads_course_offerings_OfferingId] FOREIGN KEY ([OfferingId]) REFERENCES [course_offerings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    CREATE TABLE [content_videos] (
        [Id] uniqueidentifier NOT NULL,
        [ModuleId] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [StorageUrl] nvarchar(1000) NULL,
        [EmbedUrl] nvarchar(1000) NULL,
        [DurationSeconds] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_content_videos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_content_videos_course_content_modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [course_content_modules] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    CREATE TABLE [discussion_replies] (
        [Id] uniqueidentifier NOT NULL,
        [ThreadId] uniqueidentifier NOT NULL,
        [AuthorId] uniqueidentifier NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_discussion_replies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_discussion_replies_discussion_threads_ThreadId] FOREIGN KEY ([ThreadId]) REFERENCES [discussion_threads] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_content_videos_ModuleId' AND [object_id] = OBJECT_ID(N'[content_videos]'))
    BEGIN
        CREATE INDEX [IX_content_videos_ModuleId] ON [content_videos] ([ModuleId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_course_announcements_OfferingId' AND [object_id] = OBJECT_ID(N'[course_announcements]'))
    BEGIN
        CREATE INDEX [IX_course_announcements_OfferingId] ON [course_announcements] ([OfferingId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_course_content_modules_OfferingId' AND [object_id] = OBJECT_ID(N'[course_content_modules]'))
    BEGIN
        CREATE INDEX [IX_course_content_modules_OfferingId] ON [course_content_modules] ([OfferingId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_discussion_replies_ThreadId' AND [object_id] = OBJECT_ID(N'[discussion_replies]'))
    BEGIN
        CREATE INDEX [IX_discussion_replies_ThreadId] ON [discussion_replies] ([ThreadId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_discussion_threads_OfferingId' AND [object_id] = OBJECT_ID(N'[discussion_threads]'))
    BEGIN
        CREATE INDEX [IX_discussion_threads_OfferingId] ON [discussion_threads] ([OfferingId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508050712_Phase20_LMS'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260508050712_Phase20_LMS', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508054215_Phase21_StudyPlanner'
)
BEGIN
    ALTER TABLE [academic_programs] ADD [MaxCreditLoadPerSemester] int NOT NULL DEFAULT 18;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508054215_Phase21_StudyPlanner'
)
BEGIN
    CREATE TABLE [study_plans] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [PlannedSemesterName] nvarchar(100) NOT NULL,
        [Notes] nvarchar(2000) NULL,
        [AdvisorStatus] int NOT NULL,
        [AdvisorNotes] nvarchar(2000) NULL,
        [ReviewedByUserId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_study_plans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_study_plans_student_profiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [student_profiles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508054215_Phase21_StudyPlanner'
)
BEGIN
    CREATE TABLE [study_plan_courses] (
        [Id] uniqueidentifier NOT NULL,
        [StudyPlanId] uniqueidentifier NOT NULL,
        [CourseId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_study_plan_courses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_study_plan_courses_courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [courses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_study_plan_courses_study_plans_StudyPlanId] FOREIGN KEY ([StudyPlanId]) REFERENCES [study_plans] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508054215_Phase21_StudyPlanner'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_study_plan_courses_CourseId' AND [object_id] = OBJECT_ID(N'[study_plan_courses]'))
    BEGIN
        CREATE INDEX [IX_study_plan_courses_CourseId] ON [study_plan_courses] ([CourseId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508054215_Phase21_StudyPlanner'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UQ_study_plan_courses_plan_course' AND [object_id] = OBJECT_ID(N'[study_plan_courses]'))
    BEGIN
        CREATE UNIQUE INDEX [UQ_study_plan_courses_plan_course] ON [study_plan_courses] ([StudyPlanId], [CourseId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508054215_Phase21_StudyPlanner'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_study_plans_StudentProfileId' AND [object_id] = OBJECT_ID(N'[study_plans]'))
    BEGIN
        CREATE INDEX [IX_study_plans_StudentProfileId] ON [study_plans] ([StudentProfileId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508054215_Phase21_StudyPlanner'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260508054215_Phase21_StudyPlanner', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508132355_Phase22_ExternalIntegrations'
)
BEGIN
    CREATE TABLE [accreditation_templates] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Format] nvarchar(10) NOT NULL,
        [FieldMappingsJson] nvarchar(2000) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_accreditation_templates] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508132355_Phase22_ExternalIntegrations'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_accreditation_templates_name' AND [object_id] = OBJECT_ID(N'[accreditation_templates]'))
    BEGIN
        CREATE INDEX [IX_accreditation_templates_name] ON [accreditation_templates] ([Name]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508132355_Phase22_ExternalIntegrations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260508132355_Phase22_ExternalIntegrations', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508152906_Phase25_AcademicEngineUnification'
)
BEGIN
    CREATE TABLE [institution_grading_profiles] (
        [Id] uniqueidentifier NOT NULL,
        [InstitutionType] int NOT NULL,
        [PassThreshold] decimal(5,2) NOT NULL,
        [GradeRangesJson] nvarchar(max) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_institution_grading_profiles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508152906_Phase25_AcademicEngineUnification'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_institution_grading_profiles_type' AND [object_id] = OBJECT_ID(N'[institution_grading_profiles]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_institution_grading_profiles_type] ON [institution_grading_profiles] ([InstitutionType]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260508152906_Phase25_AcademicEngineUnification'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260508152906_Phase25_AcademicEngineUnification', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE TABLE [bulk_promotion_batches] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(180) NOT NULL,
        [Status] int NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [ApprovedByUserId] uniqueidentifier NULL,
        [ReviewedAt] datetime2 NULL,
        [AppliedAt] datetime2 NULL,
        [ReviewNote] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_bulk_promotion_batches] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE TABLE [bulk_promotion_entries] (
        [Id] uniqueidentifier NOT NULL,
        [BatchId] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [Decision] int NOT NULL,
        [Reason] nvarchar(500) NULL,
        [IsApplied] bit NOT NULL,
        [AppliedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_bulk_promotion_entries] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE TABLE [parent_student_links] (
        [Id] uniqueidentifier NOT NULL,
        [ParentUserId] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [Relationship] nvarchar(60) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_parent_student_links] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE TABLE [school_streams] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_school_streams] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE TABLE [student_report_cards] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [InstitutionType] int NOT NULL,
        [PeriodLabel] nvarchar(80) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        [GeneratedByUserId] uniqueidentifier NOT NULL,
        [GeneratedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_student_report_cards] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    CREATE TABLE [student_stream_assignments] (
        [Id] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [SchoolStreamId] uniqueidentifier NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        [AssignedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_student_stream_assignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_student_stream_assignments_school_streams_SchoolStreamId] FOREIGN KEY ([SchoolStreamId]) REFERENCES [school_streams] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_bulk_promotion_batches_status_created' AND [object_id] = OBJECT_ID(N'[bulk_promotion_batches]'))
    BEGIN
        CREATE INDEX [IX_bulk_promotion_batches_status_created] ON [bulk_promotion_batches] ([Status], [CreatedAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_bulk_promotion_entries_batch' AND [object_id] = OBJECT_ID(N'[bulk_promotion_entries]'))
    BEGIN
        CREATE INDEX [IX_bulk_promotion_entries_batch] ON [bulk_promotion_entries] ([BatchId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_bulk_promotion_entries_batch_student' AND [object_id] = OBJECT_ID(N'[bulk_promotion_entries]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_bulk_promotion_entries_batch_student] ON [bulk_promotion_entries] ([BatchId], [StudentProfileId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_parent_student_links_parent_student' AND [object_id] = OBJECT_ID(N'[parent_student_links]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_parent_student_links_parent_student] ON [parent_student_links] ([ParentUserId], [StudentProfileId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_school_streams_name' AND [object_id] = OBJECT_ID(N'[school_streams]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_school_streams_name] ON [school_streams] ([Name]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_student_report_cards_student_generated' AND [object_id] = OBJECT_ID(N'[student_report_cards]'))
    BEGIN
        CREATE INDEX [IX_student_report_cards_student_generated] ON [student_report_cards] ([StudentProfileId], [GeneratedAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_student_stream_assignments_SchoolStreamId' AND [object_id] = OBJECT_ID(N'[student_stream_assignments]'))
    BEGIN
        CREATE INDEX [IX_student_stream_assignments_SchoolStreamId] ON [student_stream_assignments] ([SchoolStreamId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_student_stream_assignments_student' AND [object_id] = OBJECT_ID(N'[student_stream_assignments]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_student_stream_assignments_student] ON [student_stream_assignments] ([StudentProfileId]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509044437_Phase26_SchoolCollegeExpansion'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260509044437_Phase26_SchoolCollegeExpansion', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    DROP INDEX [IX_graduation_applications_StudentProfileId] ON [graduation_applications];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_user_sessions_user_created_at' AND [object_id] = OBJECT_ID(N'[user_sessions]'))
    BEGIN
        CREATE INDEX [IX_user_sessions_user_created_at] ON [user_sessions] ([UserId], [CreatedAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_support_tickets_assigned_created_at' AND [object_id] = OBJECT_ID(N'[support_tickets]'))
    BEGIN
        CREATE INDEX [IX_support_tickets_assigned_created_at] ON [support_tickets] ([AssignedToId], [CreatedAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_support_tickets_department_status_created_at' AND [object_id] = OBJECT_ID(N'[support_tickets]'))
    BEGIN
        CREATE INDEX [IX_support_tickets_department_status_created_at] ON [support_tickets] ([DepartmentId], [Status], [CreatedAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_support_tickets_submitter_created_at' AND [object_id] = OBJECT_ID(N'[support_tickets]'))
    BEGIN
        CREATE INDEX [IX_support_tickets_submitter_created_at] ON [support_tickets] ([SubmitterId], [CreatedAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_quiz_attempts_quiz_student_started_at' AND [object_id] = OBJECT_ID(N'[quiz_attempts]'))
    BEGIN
        CREATE INDEX [IX_quiz_attempts_quiz_student_started_at] ON [quiz_attempts] ([QuizId], [StudentProfileId], [StartedAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_quiz_attempts_student_started_at' AND [object_id] = OBJECT_ID(N'[quiz_attempts]'))
    BEGIN
        CREATE INDEX [IX_quiz_attempts_student_started_at] ON [quiz_attempts] ([StudentProfileId], [StartedAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'ix_pr_status_due_date' AND [object_id] = OBJECT_ID(N'[payment_receipts]'))
    BEGIN
        CREATE INDEX [ix_pr_status_due_date] ON [payment_receipts] ([Status], [DueDate]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'ix_pr_student_created_at' AND [object_id] = OBJECT_ID(N'[payment_receipts]'))
    BEGIN
        CREATE INDEX [ix_pr_student_created_at] ON [payment_receipts] ([StudentProfileId], [CreatedAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_notification_recipients_user_created_at' AND [object_id] = OBJECT_ID(N'[notification_recipients]'))
    BEGIN
        CREATE INDEX [IX_notification_recipients_user_created_at] ON [notification_recipients] ([RecipientUserId], [CreatedAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_graduation_applications_status_created_at' AND [object_id] = OBJECT_ID(N'[graduation_applications]'))
    BEGIN
        CREATE INDEX [IX_graduation_applications_status_created_at] ON [graduation_applications] ([Status], [CreatedAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_graduation_applications_student_created_at' AND [object_id] = OBJECT_ID(N'[graduation_applications]'))
    BEGIN
        CREATE INDEX [IX_graduation_applications_student_created_at] ON [graduation_applications] ([StudentProfileId], [CreatedAt]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260509155457_20260510_Phase29_IndexBaseline'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260509155457_20260510_Phase29_IndexBaseline', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512043929_AddUserInstitutionTypeAssignment'
)
BEGIN
    ALTER TABLE [users] ADD [InstitutionType] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512043929_AddUserInstitutionTypeAssignment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260512043929_AddUserInstitutionTypeAssignment', N'8.0.8');
END;
GO

COMMIT;
GO


