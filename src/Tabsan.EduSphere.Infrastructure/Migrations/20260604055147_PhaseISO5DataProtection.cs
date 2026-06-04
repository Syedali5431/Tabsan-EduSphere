using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PhaseISO5DataProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConsentToMonitoring",
                table: "users",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataRetentionDate",
                table: "users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "data_classification_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClassificationLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClassifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_classification_entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_data_classification_entity",
                table: "data_classification_entries",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_data_classification_level_classified",
                table: "data_classification_entries",
                columns: new[] { "ClassificationLevel", "ClassifiedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "data_classification_entries");

            migrationBuilder.DropColumn(
                name: "ConsentToMonitoring",
                table: "users");

            migrationBuilder.DropColumn(
                name: "DataRetentionDate",
                table: "users");
        }
    }
}
