using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tabsan.EduSphere.Infrastructure.Persistence;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260525001000_PlanL_TwoFactorSecretColumnWidening")]
public partial class PlanL_TwoFactorSecretColumnWidening : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "MfaTotpSecret",
            table: "users",
            type: "nvarchar(512)",
            maxLength: 512,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(128)",
            oldMaxLength: 128,
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "MfaTotpSecret",
            table: "users",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(512)",
            oldMaxLength: 512,
            oldNullable: true);
    }
}
