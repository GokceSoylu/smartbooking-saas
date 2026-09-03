using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Tenants_TenantId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Tenants_TenantId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Tenants_TenantId",
                table: "Services");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffMembers_Tenants_TenantId",
                table: "StaffMembers");

            migrationBuilder.DropIndex(
                name: "IX_StaffMembers_TenantId",
                table: "StaffMembers");

            migrationBuilder.DropIndex(
                name: "IX_Services_TenantId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_TenantId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "WhatsAppPhoneNumberId",
                table: "Tenants");

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOwnerOnNewAppointment",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CustomerWantsWhatsAppNotification",
                table: "Appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyOwnerOnNewAppointment",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CustomerWantsWhatsAppNotification",
                table: "Appointments");

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppPhoneNumberId",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_TenantId",
                table: "StaffMembers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_TenantId",
                table: "Services",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TenantId",
                table: "Appointments",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Tenants_TenantId",
                table: "Appointments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Tenants_TenantId",
                table: "Customers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Tenants_TenantId",
                table: "Services",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffMembers_Tenants_TenantId",
                table: "StaffMembers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
