using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tabsan.EduSphere.Infrastructure.Persistence;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260524093000_Phase48_BuildingTenantCampusScope")]
    public partial class Phase48_BuildingTenantCampusScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_buildings_code",
                table: "buildings");

            migrationBuilder.AddColumn<Guid>(
                name: "CampusId",
                table: "buildings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "buildings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_buildings_scope_code",
                table: "buildings",
                columns: new[] { "TenantId", "CampusId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_buildings_scope_code",
                table: "buildings");

            migrationBuilder.DropColumn(
                name: "CampusId",
                table: "buildings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "buildings");

            migrationBuilder.CreateIndex(
                name: "IX_buildings_code",
                table: "buildings",
                column: "Code",
                unique: true);
        }
    }
}
