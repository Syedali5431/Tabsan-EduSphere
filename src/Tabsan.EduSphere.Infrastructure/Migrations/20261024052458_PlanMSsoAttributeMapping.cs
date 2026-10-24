using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlanMSsoAttributeMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdClaim",
                table: "sso_provider_settings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LinkById",
                table: "sso_provider_settings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireEncryptedAssertions",
                table: "sso_provider_settings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireRoleMatch",
                table: "sso_provider_settings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RoleClaim",
                table: "sso_provider_settings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleMappings",
                table: "sso_provider_settings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SignAuthnRequests",
                table: "sso_provider_settings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdClaim",
                table: "sso_provider_settings");

            migrationBuilder.DropColumn(
                name: "LinkById",
                table: "sso_provider_settings");

            migrationBuilder.DropColumn(
                name: "RequireEncryptedAssertions",
                table: "sso_provider_settings");

            migrationBuilder.DropColumn(
                name: "RequireRoleMatch",
                table: "sso_provider_settings");

            migrationBuilder.DropColumn(
                name: "RoleClaim",
                table: "sso_provider_settings");

            migrationBuilder.DropColumn(
                name: "RoleMappings",
                table: "sso_provider_settings");

            migrationBuilder.DropColumn(
                name: "SignAuthnRequests",
                table: "sso_provider_settings");
        }
    }
}
