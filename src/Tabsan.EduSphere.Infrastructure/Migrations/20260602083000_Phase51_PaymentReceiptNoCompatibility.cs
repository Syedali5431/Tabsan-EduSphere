using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tabsan.EduSphere.Infrastructure.Persistence;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    /// <summary>
    /// Ensures payment_receipts has ReceiptNo and its unique index in environments
    /// where schema drift left the column missing.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260602083000_Phase51_PaymentReceiptNoCompatibility")]
    public partial class Phase51_PaymentReceiptNoCompatibility : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[payment_receipts]') IS NOT NULL
                   AND COL_LENGTH(N'payment_receipts', N'ReceiptNo') IS NULL
                BEGIN
                    EXEC(N'ALTER TABLE [payment_receipts] ADD [ReceiptNo] nvarchar(64) NULL;');
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[payment_receipts]') IS NOT NULL
                   AND COL_LENGTH(N'payment_receipts', N'ReceiptNo') IS NOT NULL
                BEGIN
                    EXEC(N'
                        UPDATE [payment_receipts]
                        SET [ReceiptNo] = CONCAT(N''RCPT-'', REPLACE(CONVERT(nvarchar(36), [Id]), N''-'', N''''))
                        WHERE [ReceiptNo] IS NULL;
                    ');

                    EXEC(N'ALTER TABLE [payment_receipts] ALTER COLUMN [ReceiptNo] nvarchar(64) NOT NULL;');
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[payment_receipts]') IS NOT NULL
                   AND COL_LENGTH(N'payment_receipts', N'ReceiptNo') IS NOT NULL
                   AND NOT EXISTS (
                       SELECT 1
                       FROM sys.indexes
                       WHERE [name] = N'ix_pr_receipt_no'
                         AND [object_id] = OBJECT_ID(N'[payment_receipts]'))
                BEGIN
                                        EXEC(N'CREATE UNIQUE INDEX [ix_pr_receipt_no] ON [payment_receipts] ([ReceiptNo]);');
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[payment_receipts]') IS NOT NULL
                   AND EXISTS (
                       SELECT 1
                       FROM sys.indexes
                       WHERE [name] = N'ix_pr_receipt_no'
                         AND [object_id] = OBJECT_ID(N'[payment_receipts]'))
                BEGIN
                                        EXEC(N'DROP INDEX [ix_pr_receipt_no] ON [payment_receipts];');
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[payment_receipts]') IS NOT NULL
                   AND COL_LENGTH(N'payment_receipts', N'ReceiptNo') IS NOT NULL
                BEGIN
                    EXEC(N'ALTER TABLE [payment_receipts] DROP COLUMN [ReceiptNo];');
                END
                """);
        }
    }
}
