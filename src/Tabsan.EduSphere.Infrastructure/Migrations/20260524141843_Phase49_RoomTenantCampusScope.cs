using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase49_RoomTenantCampusScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rooms_building_number",
                table: "rooms");

            migrationBuilder.AddColumn<Guid>(
                name: "CampusId",
                table: "rooms",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "rooms",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_rooms_scope_building_number",
                table: "rooms",
                columns: new[] { "TenantId", "CampusId", "BuildingId", "Number" },
                unique: true,
                filter: "[TenantId] IS NOT NULL AND [CampusId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rooms_scope_building_number",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "CampusId",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "rooms");

            migrationBuilder.CreateIndex(
                name: "IX_rooms_building_number",
                table: "rooms",
                columns: new[] { "BuildingId", "Number" },
                unique: true);
        }
    }
}
