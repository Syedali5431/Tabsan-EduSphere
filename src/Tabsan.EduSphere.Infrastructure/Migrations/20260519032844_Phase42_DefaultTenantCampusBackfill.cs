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
                IF NOT EXISTS (SELECT 1 FROM [tenants] WHERE [Id] = '{DefaultTenantId}')
                BEGIN
                    INSERT INTO [tenants] ([Id], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
                    VALUES ('{DefaultTenantId}', 'DEFAULT', 'Default Tenant', 1, SYSUTCDATETIME(), NULL, 0, NULL)
                END

                IF NOT EXISTS (SELECT 1 FROM [campuses] WHERE [Id] = '{DefaultCampusId}')
                BEGIN
                    INSERT INTO [campuses] ([Id], [TenantId], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
                    VALUES ('{DefaultCampusId}', '{DefaultTenantId}', 'MAIN', 'Main Campus', 1, SYSUTCDATETIME(), NULL, 0, NULL)
                END

                UPDATE [users]
                SET [TenantId] = '{DefaultTenantId}'
                WHERE [TenantId] IS NULL;

                UPDATE [users]
                SET [CampusId] = '{DefaultCampusId}'
                WHERE [CampusId] IS NULL;

                UPDATE [departments]
                SET [TenantId] = '{DefaultTenantId}'
                WHERE [TenantId] IS NULL;

                UPDATE [departments]
                SET [CampusId] = '{DefaultCampusId}'
                WHERE [CampusId] IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left no-op for data backfill safety.
        }
    }
}
