using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlanMPhase5Lti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExternalToolId",
                table: "assignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "lti_access_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Scopes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lti_access_tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "lti_launches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeploymentRowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResourceLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReturnUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AuthorizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lti_launches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "lti_tools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OidcInitiationUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TargetLinkUri = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DeepLinkingUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RedirectUris = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    JwksUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PublicKeyPem = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CustomParameters = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PrivacyLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AllowGradeService = table.Column<bool>(type: "bit", nullable: false),
                    AllowRosterService = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lti_tools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "lti_used_nonces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lti_used_nonces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "lti_line_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ScoreMaximum = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ResourceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lti_line_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lti_line_items_lti_tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "lti_tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lti_resource_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeploymentRowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CustomParameters = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PresentationTarget = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LineItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lti_resource_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lti_resource_links_lti_tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "lti_tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lti_tool_deployments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeploymentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lti_tool_deployments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lti_tool_deployments_lti_tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "lti_tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lti_scores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoreGiven = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    ScoreMaximum = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    ActivityProgress = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GradingProgress = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lti_scores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lti_scores_lti_line_items_LineItemId",
                        column: x => x.LineItemId,
                        principalTable: "lti_line_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lti_access_tokens_hash",
                table: "lti_access_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lti_launches_expires",
                table: "lti_launches",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_lti_line_items_tool_offering",
                table: "lti_line_items",
                columns: new[] { "ToolId", "CourseOfferingId" });

            migrationBuilder.CreateIndex(
                name: "IX_lti_resource_links_offering",
                table: "lti_resource_links",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_lti_resource_links_ToolId",
                table: "lti_resource_links",
                column: "ToolId");

            migrationBuilder.CreateIndex(
                name: "IX_lti_scores_line_item_student",
                table: "lti_scores",
                columns: new[] { "LineItemId", "StudentProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_lti_tool_deployments_deployment_id",
                table: "lti_tool_deployments",
                column: "DeploymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lti_tool_deployments_ToolId",
                table: "lti_tool_deployments",
                column: "ToolId");

            migrationBuilder.CreateIndex(
                name: "IX_lti_tools_client_id",
                table: "lti_tools",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lti_used_nonces_expires",
                table: "lti_used_nonces",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_lti_used_nonces_tool_value",
                table: "lti_used_nonces",
                columns: new[] { "ToolId", "Value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lti_access_tokens");

            migrationBuilder.DropTable(
                name: "lti_launches");

            migrationBuilder.DropTable(
                name: "lti_resource_links");

            migrationBuilder.DropTable(
                name: "lti_scores");

            migrationBuilder.DropTable(
                name: "lti_tool_deployments");

            migrationBuilder.DropTable(
                name: "lti_used_nonces");

            migrationBuilder.DropTable(
                name: "lti_line_items");

            migrationBuilder.DropTable(
                name: "lti_tools");

            migrationBuilder.DropColumn(
                name: "ExternalToolId",
                table: "assignments");
        }
    }
}
