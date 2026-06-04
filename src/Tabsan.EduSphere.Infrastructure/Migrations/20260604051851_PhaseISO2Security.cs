using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PhaseISO2Security : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_sessions_user_id",
                table: "user_sessions");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordChangedAt",
                table: "users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityAt",
                table: "user_sessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "password_history",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_active",
                table: "user_sessions",
                column: "UserId",
                filter: "[RevokedAt] IS NULL")
                .Annotation("SqlServer:Include", new[] { "ExpiresAt", "RevokedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_sessions_active",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "LastPasswordChangedAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastActivityAt",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "password_history");

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_user_id",
                table: "user_sessions",
                column: "UserId");
        }
    }
}
