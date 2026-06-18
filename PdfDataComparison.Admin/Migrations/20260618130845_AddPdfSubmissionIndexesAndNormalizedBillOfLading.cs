using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdfDataComparison.Admin.Migrations
{
    /// <inheritdoc />
    public partial class AddPdfSubmissionIndexesAndNormalizedBillOfLading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillOfLadingNumberNormalized",
                table: "PdfComparisonSubmissions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE PdfComparisonSubmissions
                SET BillOfLadingNumberNormalized = LOWER(
                    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                    BillOfLadingNumber, ' ', ''), '-', ''), '_', ''), '.', ''), '/', ''), '\', ''), ':', ''), ';', ''), ',', ''), '#', '')
                )
                WHERE BillOfLadingNumber IS NOT NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ComparisonJobs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_PdfComparisonSubmissions_IsActive_BillOfLadingNumberNormalized",
                table: "PdfComparisonSubmissions",
                columns: new[] { "IsActive", "BillOfLadingNumberNormalized" });

            migrationBuilder.CreateIndex(
                name: "IX_PdfComparisonSubmissions_IsActive_SubmittedAt",
                table: "PdfComparisonSubmissions",
                columns: new[] { "IsActive", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ComparisonJobs_Status_CreatedAt",
                table: "ComparisonJobs",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PdfComparisonSubmissions_IsActive_BillOfLadingNumberNormalized",
                table: "PdfComparisonSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PdfComparisonSubmissions_IsActive_SubmittedAt",
                table: "PdfComparisonSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_ComparisonJobs_Status_CreatedAt",
                table: "ComparisonJobs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "BillOfLadingNumberNormalized",
                table: "PdfComparisonSubmissions");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ComparisonJobs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
