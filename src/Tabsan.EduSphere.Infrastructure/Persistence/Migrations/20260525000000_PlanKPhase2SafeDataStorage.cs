using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlanKPhase2SafeDataStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "academic_document_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateType = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CampusId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_academic_document_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "degree_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcademicDocumentTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IssueDate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DocxPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PdfPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VerificationUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_degree_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_degree_documents_academic_document_templates_AcademicDocumentTemplateId",
                        column: x => x.AcademicDocumentTemplateId,
                        principalTable: "academic_document_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_degree_documents_student_profiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "student_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transcript_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcademicDocumentTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IssueDate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DocxPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PdfPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VerificationUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CourseSnapshotJson = table.Column<string>(type: "nvarchar(8000)", maxLength: 8000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transcript_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transcript_documents_academic_document_templates_AcademicDocumentTemplateId",
                        column: x => x.AcademicDocumentTemplateId,
                        principalTable: "academic_document_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_transcript_documents_student_profiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "student_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_academic_document_templates_type_active",
                table: "academic_document_templates",
                columns: new[] { "TemplateType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_academic_document_templates_type_version",
                table: "academic_document_templates",
                columns: new[] { "TemplateType", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_degree_documents_student_generated_at",
                table: "degree_documents",
                columns: new[] { "StudentProfileId", "GeneratedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_degree_documents_student_id",
                table: "degree_documents",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_degree_documents_AcademicDocumentTemplateId",
                table: "degree_documents",
                column: "AcademicDocumentTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_transcript_documents_student_generated_at",
                table: "transcript_documents",
                columns: new[] { "StudentProfileId", "GeneratedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_transcript_documents_student_id",
                table: "transcript_documents",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_transcript_documents_AcademicDocumentTemplateId",
                table: "transcript_documents",
                column: "AcademicDocumentTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "degree_documents");
            migrationBuilder.DropTable(name: "transcript_documents");
            migrationBuilder.DropTable(name: "academic_document_templates");
        }
    }
}