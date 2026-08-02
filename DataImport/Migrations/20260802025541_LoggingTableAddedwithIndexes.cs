using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataImport.Migrations
{
    /// <inheritdoc />
    public partial class LoggingTableAddedwithIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataImportLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RanAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Parsed = table.Column<int>(type: "int", nullable: false),
                    Inserted = table.Column<int>(type: "int", nullable: false),
                    Updated = table.Column<int>(type: "int", nullable: false),
                    Unchanged = table.Column<int>(type: "int", nullable: false),
                    WasDownloaded = table.Column<bool>(type: "bit", nullable: false),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataImportLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataImportLogs_RanAtUtc",
                table: "DataImportLogs",
                column: "RanAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DataImportLogs_Succeeded_RanAtUtc",
                table: "DataImportLogs",
                columns: new[] { "Succeeded", "RanAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataImportLogs");
        }
    }
}
