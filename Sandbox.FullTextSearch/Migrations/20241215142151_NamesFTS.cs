using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sandbox.FullTextSearch.Migrations
{
    /// <inheritdoc />
    public partial class NamesFTS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE FULLTEXT INDEX ON Names (FirstName, LastName LANGUAGE 1033)
KEY INDEX PK_Names;
", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP FULLTEXT INDEX ON Users;
", suppressTransaction: true);
        }
    }
}
