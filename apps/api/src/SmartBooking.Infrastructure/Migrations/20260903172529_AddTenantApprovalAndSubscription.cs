using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantApprovalAndSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionExpiresAtUtc",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SubscriptionExpiresAtUtc",
                table: "Tenants");
        }
    }
}
