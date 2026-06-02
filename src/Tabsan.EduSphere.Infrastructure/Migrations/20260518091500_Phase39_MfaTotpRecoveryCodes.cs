using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Tabsan.EduSphere.Infrastructure.Persistence;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260518091500_Phase39_MfaTotpRecoveryCodes")]
public partial class Phase39_MfaTotpRecoveryCodes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            @"IF COL_LENGTH('users', 'MfaIsEnabled') IS NULL
BEGIN
    ALTER TABLE [users] ADD [MfaIsEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);
END");

        migrationBuilder.Sql(
            @"IF COL_LENGTH('users', 'MfaRecoveryCodesHashJson') IS NULL
BEGIN
    ALTER TABLE [users] ADD [MfaRecoveryCodesHashJson] nvarchar(4000) NULL;
END");

        migrationBuilder.Sql(
            @"IF COL_LENGTH('users', 'MfaTotpSecret') IS NULL
BEGIN
    ALTER TABLE [users] ADD [MfaTotpSecret] nvarchar(128) NULL;
END");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            @"IF COL_LENGTH('users', 'MfaIsEnabled') IS NOT NULL
BEGIN
    ALTER TABLE [users] DROP COLUMN [MfaIsEnabled];
END");

        migrationBuilder.Sql(
            @"IF COL_LENGTH('users', 'MfaRecoveryCodesHashJson') IS NOT NULL
BEGIN
    ALTER TABLE [users] DROP COLUMN [MfaRecoveryCodesHashJson];
END");

        migrationBuilder.Sql(
            @"IF COL_LENGTH('users', 'MfaTotpSecret') IS NOT NULL
BEGIN
    ALTER TABLE [users] DROP COLUMN [MfaTotpSecret];
END");
    }
}
