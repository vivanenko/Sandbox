using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sandbox.FullTextSearch.Migrations
{
    /// <inheritdoc />
    public partial class NotificationsFTS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE FULLTEXT INDEX ON Notifications (Subject, Content LANGUAGE 1033)
KEY INDEX PK_Notifications;
", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP FULLTEXT INDEX ON Notifications;
", suppressTransaction: true);
        }
    }
}
