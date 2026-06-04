using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PhaseISO1AuditLoggingPart2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "audit_logs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceInfo",
                table: "audit_logs",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventCategory",
                table: "audit_logs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "audit_logs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Info");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_actor_role_occurred_at",
                table: "audit_logs",
                columns: new[] { "ActorRole", "OccurredAt" },
                filter: "[ActorRole] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_correlation_id",
                table: "audit_logs",
                column: "CorrelationId",
                filter: "[CorrelationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_event_category",
                table: "audit_logs",
                column: "EventCategory",
                filter: "[EventCategory] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_severity_occurred_at",
                table: "audit_logs",
                columns: new[] { "Severity", "OccurredAt" },
                filter: "[Severity] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_audit_logs_actor_role_occurred_at",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_correlation_id",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_event_category",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_severity_occurred_at",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "DeviceInfo",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "EventCategory",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "audit_logs");
        }
    }
}
