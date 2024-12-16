using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sandbox.FullTextSearch.Migrations
{
    /// <inheritdoc />
    public partial class NotificationAndEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEmail",
                table: "Notifications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsNotification",
                table: "Notifications",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEmail",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "IsNotification",
                table: "Notifications");
        }
    }
}
