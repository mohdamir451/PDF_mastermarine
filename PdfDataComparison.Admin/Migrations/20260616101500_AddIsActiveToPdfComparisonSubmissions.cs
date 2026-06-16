using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdfDataComparison.Admin.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToPdfComparisonSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PdfComparisonSubmissions",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PdfComparisonSubmissions");
        }
    }
}
