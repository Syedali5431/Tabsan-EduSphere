using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase31_Stage31_3_DiscussionEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns to discussion_threads table for type classification, resolution tracking, ticket system, and visibility
            migrationBuilder.AddColumn<string>(
                name: "ThreadType",
                table: "discussion_threads",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Issue");

            migrationBuilder.AddColumn<string>(
                name: "IssueSubType",
                table: "discussion_threads",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSolved",
                table: "discussion_threads",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ResolvedBy",
                table: "discussion_threads",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "discussion_threads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TicketNumber",
                table: "discussion_threads",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsVisibleToAll",
                table: "discussion_threads",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Create an index on TicketNumber for quick lookup
            migrationBuilder.CreateIndex(
                name: "IX_discussion_threads_TicketNumber",
                table: "discussion_threads",
                column: "TicketNumber",
                unique: true);

            // Create an index on IsSolved for filtering resolved discussions
            migrationBuilder.CreateIndex(
                name: "IX_discussion_threads_IsSolved",
                table: "discussion_threads",
                column: "IsSolved");

            // Create an index on IsVisibleToAll for department-wide visibility queries
            migrationBuilder.CreateIndex(
                name: "IX_discussion_threads_IsVisibleToAll",
                table: "discussion_threads",
                column: "IsVisibleToAll");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_discussion_threads_TicketNumber",
                table: "discussion_threads");

            migrationBuilder.DropIndex(
                name: "IX_discussion_threads_IsSolved",
                table: "discussion_threads");

            migrationBuilder.DropIndex(
                name: "IX_discussion_threads_IsVisibleToAll",
                table: "discussion_threads");

            migrationBuilder.DropColumn(
                name: "ThreadType",
                table: "discussion_threads");

            migrationBuilder.DropColumn(
                name: "IssueSubType",
                table: "discussion_threads");

            migrationBuilder.DropColumn(
                name: "IsSolved",
                table: "discussion_threads");

            migrationBuilder.DropColumn(
                name: "ResolvedBy",
                table: "discussion_threads");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "discussion_threads");

            migrationBuilder.DropColumn(
                name: "TicketNumber",
                table: "discussion_threads");

            migrationBuilder.DropColumn(
                name: "IsVisibleToAll",
                table: "discussion_threads");
        }
    }
}
