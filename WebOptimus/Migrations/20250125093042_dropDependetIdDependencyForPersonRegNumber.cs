using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class dropDependetIdDependencyForPersonRegNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "PersonRegNumber",
                table: "ReportedDeath",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "personRegNumber",
                table: "PaymentSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "personRegNumber",
                table: "Payment",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "personRegNumber",
                table: "PasswordResets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PersonRegNumber",
                table: "OtherDonationPayment",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PersonRegNumber",
                table: "NextOfKins",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "personRegNumber",
                table: "Groups",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PersonRegNumber",
                table: "GroupMembers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PersonRegNumber",
                table: "EmailVerifications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PersonRegNumber",
                table: "DonationForNonDeathRelated",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PersonRegNumber",
                table: "DependentChecklistItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "PersonRegNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonRegNumber",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "personRegNumber",
                table: "PaymentSessions");

            migrationBuilder.DropColumn(
                name: "personRegNumber",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "personRegNumber",
                table: "PasswordResets");

            migrationBuilder.DropColumn(
                name: "PersonRegNumber",
                table: "OtherDonationPayment");

            migrationBuilder.DropColumn(
                name: "PersonRegNumber",
                table: "NextOfKins");

            migrationBuilder.DropColumn(
                name: "personRegNumber",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "PersonRegNumber",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "PersonRegNumber",
                table: "EmailVerifications");

            migrationBuilder.DropColumn(
                name: "PersonRegNumber",
                table: "DonationForNonDeathRelated");

            migrationBuilder.DropColumn(
                name: "PersonRegNumber",
                table: "DependentChecklistItems");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "PersonRegNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AspNetUsers",
                type: "bit",
                nullable: true);
        }
    }
}
