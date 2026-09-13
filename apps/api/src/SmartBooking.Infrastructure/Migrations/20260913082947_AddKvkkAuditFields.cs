using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKvkkAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KvkkApprovalIpAddress",
                table: "Appointments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "KvkkApprovedAtUtc",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KvkkConsentTextVersion",
                table: "Appointments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KvkkApprovalIpAddress",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "KvkkApprovedAtUtc",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "KvkkConsentTextVersion",
                table: "Appointments");
        }
    }
}
