using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase42_DefaultTenantCampusBackfill : Migration
    {
        private const string DefaultTenantId = "11111111-1111-1111-1111-111111111111";
        private const string DefaultCampusId = "22222222-2222-2222-2222-222222222222";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                DECLARE @DefaultTenantId uniqueidentifier = '{DefaultTenantId}';
                DECLARE @DefaultCampusId uniqueidentifier = '{DefaultCampusId}';
                DECLARE @EffectiveTenantId uniqueidentifier;
                DECLARE @EffectiveCampusId uniqueidentifier;

                -- Reuse existing DEFAULT tenant by code if present, otherwise create canonical row.
                SELECT TOP (1) @EffectiveTenantId = [Id]
                FROM [tenants]
                WHERE [Code] = N'DEFAULT'
                ORDER BY CASE WHEN [Id] = @DefaultTenantId THEN 0 ELSE 1 END;

                IF @EffectiveTenantId IS NULL
                BEGIN
                    SET @EffectiveTenantId = @DefaultTenantId;

                    INSERT INTO [tenants] ([Id], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
                    VALUES (@EffectiveTenantId, N'DEFAULT', N'Default Tenant', 1, SYSUTCDATETIME(), NULL, 0, NULL);
                END

                -- Reuse existing MAIN campus under effective tenant if present.
                SELECT TOP (1) @EffectiveCampusId = [Id]
                FROM [campuses]
                WHERE [TenantId] = @EffectiveTenantId
                  AND [Code] = N'MAIN'
                ORDER BY CASE WHEN [Id] = @DefaultCampusId THEN 0 ELSE 1 END;

                IF @EffectiveCampusId IS NULL
                BEGIN
                    -- Prefer canonical campus id when available; otherwise generate a new safe id.
                    IF NOT EXISTS (SELECT 1 FROM [campuses] WHERE [Id] = @DefaultCampusId)
                        SET @EffectiveCampusId = @DefaultCampusId;
                    ELSE
                        SET @EffectiveCampusId = NEWID();

                    INSERT INTO [campuses] ([Id], [TenantId], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
                    VALUES (@EffectiveCampusId, @EffectiveTenantId, N'MAIN', N'Main Campus', 1, SYSUTCDATETIME(), NULL, 0, NULL);
                END

                -- Update tenant/campus in one statement to satisfy CK_users_tenant_campus_pair.
                UPDATE u
                SET
                    [TenantId] = COALESCE(u.[TenantId], c.[TenantId], @EffectiveTenantId),
                    [CampusId] = COALESCE(
                        u.[CampusId],
                        preferredCampus.[Id],
                        @EffectiveCampusId)
                FROM [users] u
                LEFT JOIN [campuses] c ON c.[Id] = u.[CampusId]
                OUTER APPLY (
                    SELECT TOP (1) c2.[Id]
                    FROM [campuses] c2
                    WHERE c2.[TenantId] = COALESCE(u.[TenantId], c.[TenantId], @EffectiveTenantId)
                    ORDER BY CASE WHEN c2.[Code] = N'MAIN' THEN 0 ELSE 1 END, c2.[CreatedAt]
                ) preferredCampus
                WHERE u.[TenantId] IS NULL OR u.[CampusId] IS NULL;

                -- Same atomic pair update for departments.
                UPDATE d
                SET
                    [TenantId] = COALESCE(d.[TenantId], c.[TenantId], @EffectiveTenantId),
                    [CampusId] = COALESCE(
                        d.[CampusId],
                        preferredCampus.[Id],
                        @EffectiveCampusId)
                FROM [departments] d
                LEFT JOIN [campuses] c ON c.[Id] = d.[CampusId]
                OUTER APPLY (
                    SELECT TOP (1) c2.[Id]
                    FROM [campuses] c2
                    WHERE c2.[TenantId] = COALESCE(d.[TenantId], c.[TenantId], @EffectiveTenantId)
                    ORDER BY CASE WHEN c2.[Code] = N'MAIN' THEN 0 ELSE 1 END, c2.[CreatedAt]
                ) preferredCampus
                WHERE d.[TenantId] IS NULL OR d.[CampusId] IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left no-op for data backfill safety.
        }
    }
}
