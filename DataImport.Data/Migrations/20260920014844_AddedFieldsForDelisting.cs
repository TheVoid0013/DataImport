using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataImport.Migrations
{
    /// <inheritdoc />
    public partial class AddedFieldsForDelisting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "SanctionDetails",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "SanctionDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAtUtc",
                table: "SanctionDetails",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "RemovedAtUtc",
                table: "SanctionDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Removed",
                table: "DataImportLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SanctionDetails_IsActive_SdnType_LastName",
                table: "SanctionDetails",
                columns: new[] { "IsActive", "SdnType", "LastName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SanctionDetails_IsActive_SdnType_LastName",
                table: "SanctionDetails");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "SanctionDetails");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "SanctionDetails");

            migrationBuilder.DropColumn(
                name: "LastSeenAtUtc",
                table: "SanctionDetails");

            migrationBuilder.DropColumn(
                name: "RemovedAtUtc",
                table: "SanctionDetails");

            migrationBuilder.DropColumn(
                name: "Removed",
                table: "DataImportLogs");
        }
    }
}
