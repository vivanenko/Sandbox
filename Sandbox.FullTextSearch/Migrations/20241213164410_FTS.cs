using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sandbox.FullTextSearch.Migrations
{
    /// <inheritdoc />
    public partial class FTS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE FULLTEXT CATALOG FullTextCatalog AS DEFAULT;
CREATE FULLTEXT INDEX ON Users (FirstName, LastName LANGUAGE 1033)
KEY INDEX PK_Users;
", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP FULLTEXT INDEX ON Users;
DROP FULLTEXT CATALOG FullTextCatalog;
", suppressTransaction: true);
        }
    }
}
