using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlanMScormSequencing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SequencingJson",
                table: "scorm_packages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StructureVersion",
                table: "scorm_packages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SequencingStateJson",
                table: "scorm_attempts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SequencingJson",
                table: "scorm_packages");

            migrationBuilder.DropColumn(
                name: "StructureVersion",
                table: "scorm_packages");

            migrationBuilder.DropColumn(
                name: "SequencingStateJson",
                table: "scorm_attempts");
        }
    }
}
