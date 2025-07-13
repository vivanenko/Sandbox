using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sandbox.Ordering.Sagas.OrderPayment.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class HoldId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "OrderPaymentState");

            migrationBuilder.AddColumn<Guid>(
                name: "HoldId",
                table: "OrderPaymentState",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HoldId",
                table: "OrderPaymentState");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "OrderPaymentState",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
