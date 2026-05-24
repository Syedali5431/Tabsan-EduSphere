using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tabsan.EduSphere.Infrastructure.Persistence;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260523021000_Phase47_CourseInstitutionScope")]
    public partial class Phase47_CourseInstitutionScope : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CampusId",
                table: "courses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstitutionType",
                table: "courses",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "courses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CampusId",
                table: "course_offerings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstitutionType",
                table: "course_offerings",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "course_offerings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE c
SET
    c.TenantId = d.TenantId,
    c.CampusId = d.CampusId,
    c.InstitutionType = CAST(d.InstitutionType AS int)
FROM courses c
INNER JOIN departments d ON d.Id = c.DepartmentId;
");

            migrationBuilder.Sql(@"
UPDATE o
SET
    o.TenantId = c.TenantId,
    o.CampusId = c.CampusId,
    o.InstitutionType = c.InstitutionType
FROM course_offerings o
INNER JOIN courses c ON c.Id = o.CourseId;
");

            migrationBuilder.CreateIndex(
                name: "IX_courses_scope_active",
                table: "courses",
                columns: new[] { "TenantId", "CampusId", "InstitutionType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_course_offerings_scope_open",
                table: "course_offerings",
                columns: new[] { "TenantId", "CampusId", "InstitutionType", "IsOpen" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_courses_scope_active",
                table: "courses");

            migrationBuilder.DropIndex(
                name: "IX_course_offerings_scope_open",
                table: "course_offerings");

            migrationBuilder.DropColumn(
                name: "CampusId",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "InstitutionType",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "CampusId",
                table: "course_offerings");

            migrationBuilder.DropColumn(
                name: "InstitutionType",
                table: "course_offerings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "course_offerings");
        }
    }
}