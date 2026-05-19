using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlanCPhase1CourseMaterialFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "course_materials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    LinkUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MaterialType = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_materials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_course_materials_academic_programs_AcademicProgramId",
                        column: x => x.AcademicProgramId,
                        principalTable: "academic_programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_course_materials_campuses_CampusId_TenantId",
                        columns: x => new { x.CampusId, x.TenantId },
                        principalTable: "campuses",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_course_materials_courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_course_materials_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_course_materials_semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_course_materials_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_course_materials_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_course_materials_AcademicProgramId",
                table: "course_materials",
                column: "AcademicProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_course_materials_campus_id",
                table: "course_materials",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_course_materials_CampusId_TenantId",
                table: "course_materials",
                columns: new[] { "CampusId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_course_materials_course_semester_active",
                table: "course_materials",
                columns: new[] { "CourseId", "SemesterId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_course_materials_CreatedByUserId",
                table: "course_materials",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_course_materials_DepartmentId",
                table: "course_materials",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_course_materials_scope_lookup",
                table: "course_materials",
                columns: new[] { "TenantId", "CampusId", "DepartmentId", "AcademicProgramId", "SemesterId", "CourseId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_course_materials_SemesterId",
                table: "course_materials",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_course_materials_tenant_id",
                table: "course_materials",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_materials");
        }
    }
}
