using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tabsan.EduSphere.Infrastructure.Persistence;

#nullable disable

namespace Tabsan.EduSphere.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260603080000_PatchUsersFullNameFatherNameSafe")]
    public partial class PatchUsersFullNameFatherNameSafe : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF COL_LENGTH(N'[dbo].[users]', N'FullName') IS NULL
                                  ALTER TABLE [dbo].[users] ADD [FullName] nvarchar(200) NULL;");

            migrationBuilder.Sql(@"IF COL_LENGTH(N'[dbo].[users]', N'FatherName') IS NULL
                                  ALTER TABLE [dbo].[users] ADD [FatherName] nvarchar(200) NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF COL_LENGTH(N'[dbo].[users]', N'FatherName') IS NOT NULL
                                  ALTER TABLE [dbo].[users] DROP COLUMN [FatherName];");

            migrationBuilder.Sql(@"IF COL_LENGTH(N'[dbo].[users]', N'FullName') IS NOT NULL
                                  ALTER TABLE [dbo].[users] DROP COLUMN [FullName];");
        }
    }
}
