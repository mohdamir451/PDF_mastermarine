using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdfDataComparison.Admin.Migrations
{
    /// <inheritdoc />
    public partial class AddPdfComparisonSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PdfComparisonSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BillOfLadingNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceFileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PdfComparisonSubmissions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PdfComparisonSubmissions_BillOfLadingNumber",
                table: "PdfComparisonSubmissions",
                column: "BillOfLadingNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PdfComparisonSubmissions");
        }
    }
}
