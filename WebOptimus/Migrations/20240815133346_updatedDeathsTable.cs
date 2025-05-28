using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class updatedDeathsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CauseOfDeath",
                table: "ReportedDeath");

            migrationBuilder.RenameColumn(
                name: "RegionalAdminApprovalNote",
                table: "ReportedDeath",
                newName: "RegionalAdminNote");

            migrationBuilder.RenameColumn(
                name: "GeneralAdminApprovalNote",
                table: "ReportedDeath",
                newName: "GeneralAdminNote");

            migrationBuilder.RenameColumn(
                name: "IsDead",
                table: "Dependants",
                newName: "IsReportedDead");

            migrationBuilder.AlterColumn<string>(
                name: "PlaceOfBurial",
                table: "ReportedDeath",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsApprovedByGeneralAdmin",
                table: "ReportedDeath",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsApprovedByRegionalAdmin",
                table: "ReportedDeath",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRejectedByGeneralAdmin",
                table: "ReportedDeath",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRejectedByRegionalAdmin",
                table: "ReportedDeath",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsConfirmedDead",
                table: "Dependants",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApprovedByGeneralAdmin",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "IsApprovedByRegionalAdmin",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "IsRejectedByGeneralAdmin",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "IsRejectedByRegionalAdmin",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "IsConfirmedDead",
                table: "Dependants");

            migrationBuilder.RenameColumn(
                name: "RegionalAdminNote",
                table: "ReportedDeath",
                newName: "RegionalAdminApprovalNote");

            migrationBuilder.RenameColumn(
                name: "GeneralAdminNote",
                table: "ReportedDeath",
                newName: "GeneralAdminApprovalNote");

            migrationBuilder.RenameColumn(
                name: "IsReportedDead",
                table: "Dependants",
                newName: "IsDead");

            migrationBuilder.AlterColumn<string>(
                name: "PlaceOfBurial",
                table: "ReportedDeath",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CauseOfDeath",
                table: "ReportedDeath",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
