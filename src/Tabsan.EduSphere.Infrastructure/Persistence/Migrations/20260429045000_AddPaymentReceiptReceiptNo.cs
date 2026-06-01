using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [Migration("20260429045000_AddPaymentReceiptReceiptNo")]
    public partial class AddPaymentReceiptReceiptNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptNo",
                table: "payment_receipts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [payment_receipts]
                SET [ReceiptNo] = CONCAT(N'RCPT-', REPLACE(CONVERT(nvarchar(36), [Id]), N'-', N''))
                WHERE [ReceiptNo] IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ReceiptNo",
                table: "payment_receipts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_pr_receipt_no",
                table: "payment_receipts",
                column: "ReceiptNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_pr_receipt_no",
                table: "payment_receipts");

            migrationBuilder.DropColumn(
                name: "ReceiptNo",
                table: "payment_receipts");
        }
    }
}
