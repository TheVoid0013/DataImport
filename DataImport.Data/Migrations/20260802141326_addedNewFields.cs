using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataImport.Migrations
{
    /// <inheritdoc />
    public partial class addedNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "SanctionDetails",
                type: "nvarchar(350)",
                maxLength: 350,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "SanctionDetails",
                type: "nvarchar(350)",
                maxLength: 350,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SdnType",
                table: "SanctionDetails",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SanctionDetails_SdnType_LastName",
                table: "SanctionDetails",
                columns: new[] { "SdnType", "LastName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SanctionDetails_SdnType_LastName",
                table: "SanctionDetails");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "SanctionDetails");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "SanctionDetails");

            migrationBuilder.DropColumn(
                name: "SdnType",
                table: "SanctionDetails");
        }
    }
}
