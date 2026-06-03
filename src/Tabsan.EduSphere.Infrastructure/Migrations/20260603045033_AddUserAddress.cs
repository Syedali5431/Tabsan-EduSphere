using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[result_component_rules]') AND name = N'IX_result_component_rules_name')
                                  DROP INDEX [IX_result_component_rules_name] ON [dbo].[result_component_rules];");

            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[gpa_scale_rules]') AND name = N'IX_gpa_scale_rules_minimum_score')
                                  DROP INDEX [IX_gpa_scale_rules_minimum_score] ON [dbo].[gpa_scale_rules];");

            migrationBuilder.AlterColumn<string>(
                name: "MfaTotpSecret",
                table: "users",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.Sql(@"IF COL_LENGTH(N'[dbo].[users]', N'Address') IS NULL
                                  ALTER TABLE [dbo].[users] ADD [Address] nvarchar(500) NULL;");

            migrationBuilder.Sql(@"IF COL_LENGTH(N'[dbo].[users]', N'FullName') IS NULL
                                  ALTER TABLE [dbo].[users] ADD [FullName] nvarchar(200) NULL;");

            migrationBuilder.Sql(@"IF COL_LENGTH(N'[dbo].[users]', N'FatherName') IS NULL
                                  ALTER TABLE [dbo].[users] ADD [FatherName] nvarchar(200) NULL;");

            migrationBuilder.Sql(@"IF COL_LENGTH(N'[dbo].[result_component_rules]', N'InstitutionType') IS NULL
                                  ALTER TABLE [dbo].[result_component_rules] ADD [InstitutionType] int NOT NULL CONSTRAINT [DF_result_component_rules_InstitutionType] DEFAULT (0);");

            migrationBuilder.Sql(@"IF COL_LENGTH(N'[dbo].[payment_receipts]', N'ReceiptNo') IS NULL
                                  ALTER TABLE [dbo].[payment_receipts] ADD [ReceiptNo] nvarchar(64) NOT NULL CONSTRAINT [DF_payment_receipts_ReceiptNo] DEFAULT (N'');");

            migrationBuilder.Sql(@"IF COL_LENGTH(N'[dbo].[gpa_scale_rules]', N'InstitutionType') IS NULL
                                  ALTER TABLE [dbo].[gpa_scale_rules] ADD [InstitutionType] int NOT NULL CONSTRAINT [DF_gpa_scale_rules_InstitutionType] DEFAULT (0);");

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
                    CourseSnapshotJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
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

            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[result_component_rules]') AND name = N'IX_result_component_rules_institution_name')
                                  CREATE UNIQUE INDEX [IX_result_component_rules_institution_name] ON [dbo].[result_component_rules] ([InstitutionType], [Name]);");

            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[payment_receipts]') AND name = N'ix_pr_receipt_no')
                                  CREATE UNIQUE INDEX [ix_pr_receipt_no] ON [dbo].[payment_receipts] ([ReceiptNo]);");

            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[gpa_scale_rules]') AND name = N'IX_gpa_scale_rules_institution_minimum_score')
                                  CREATE UNIQUE INDEX [IX_gpa_scale_rules_institution_minimum_score] ON [dbo].[gpa_scale_rules] ([InstitutionType], [MinimumScore]);");

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
                name: "IX_degree_documents_AcademicDocumentTemplateId",
                table: "degree_documents",
                column: "AcademicDocumentTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_degree_documents_student_generated_at",
                table: "degree_documents",
                columns: new[] { "StudentProfileId", "GeneratedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_degree_documents_student_id",
                table: "degree_documents",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_transcript_documents_AcademicDocumentTemplateId",
                table: "transcript_documents",
                column: "AcademicDocumentTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_transcript_documents_student_generated_at",
                table: "transcript_documents",
                columns: new[] { "StudentProfileId", "GeneratedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_transcript_documents_student_id",
                table: "transcript_documents",
                column: "StudentProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "degree_documents");

            migrationBuilder.DropTable(
                name: "transcript_documents");

            migrationBuilder.DropTable(
                name: "academic_document_templates");

            migrationBuilder.DropIndex(
                name: "IX_result_component_rules_institution_name",
                table: "result_component_rules");

            migrationBuilder.DropIndex(
                name: "ix_pr_receipt_no",
                table: "payment_receipts");

            migrationBuilder.DropIndex(
                name: "IX_gpa_scale_rules_institution_minimum_score",
                table: "gpa_scale_rules");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "users");

            migrationBuilder.DropColumn(
                name: "InstitutionType",
                table: "result_component_rules");

            migrationBuilder.DropColumn(
                name: "ReceiptNo",
                table: "payment_receipts");

            migrationBuilder.DropColumn(
                name: "InstitutionType",
                table: "gpa_scale_rules");

            migrationBuilder.AlterColumn<string>(
                name: "MfaTotpSecret",
                table: "users",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_result_component_rules_name",
                table: "result_component_rules",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gpa_scale_rules_minimum_score",
                table: "gpa_scale_rules",
                column: "MinimumScore",
                unique: true);
        }
    }
}
