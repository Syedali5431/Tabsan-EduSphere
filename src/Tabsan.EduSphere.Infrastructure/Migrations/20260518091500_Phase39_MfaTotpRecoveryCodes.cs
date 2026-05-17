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
        migrationBuilder.AddColumn<bool>(
            name: "MfaIsEnabled",
            table: "users",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "MfaRecoveryCodesHashJson",
            table: "users",
            type: "nvarchar(4000)",
            maxLength: 4000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MfaTotpSecret",
            table: "users",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MfaIsEnabled",
            table: "users");

        migrationBuilder.DropColumn(
            name: "MfaRecoveryCodesHashJson",
            table: "users");

        migrationBuilder.DropColumn(
            name: "MfaTotpSecret",
            table: "users");
    }
}
