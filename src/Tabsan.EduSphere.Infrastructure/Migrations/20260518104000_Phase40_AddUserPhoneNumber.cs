using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tabsan.EduSphere.Infrastructure.Persistence;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260518104000_Phase40_AddUserPhoneNumber")]
public partial class Phase40_AddUserPhoneNumber : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            @"IF COL_LENGTH('users', 'PhoneNumber') IS NULL
BEGIN
    ALTER TABLE [users] ADD [PhoneNumber] nvarchar(32) NULL;
END");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            @"IF COL_LENGTH('users', 'PhoneNumber') IS NOT NULL
BEGIN
    ALTER TABLE [users] DROP COLUMN [PhoneNumber];
END");
    }
}
