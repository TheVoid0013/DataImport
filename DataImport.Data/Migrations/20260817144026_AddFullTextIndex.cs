using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataImport.Migrations
{
    public partial class AddFullTextIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'FTCatalog')
                CREATE FULLTEXT CATALOG FTCatalog AS DEFAULT;
        ", suppressTransaction: true);

            migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('SanctionDetails'))
            CREATE FULLTEXT INDEX ON SanctionDetails(FirstName, LastName)
            KEY INDEX PK_SanctionDetails
            ON FTCatalog
            WITH CHANGE_TRACKING AUTO;
        ", suppressTransaction: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            IF EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('SanctionDetails'))
                DROP FULLTEXT INDEX ON SanctionDetails;
        ", suppressTransaction: true);
        }
    }
}
