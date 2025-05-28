using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class AddedApproveRejectedEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovedByGeneralAdmin",
                table: "ReportedDeath",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByRegionalAdmin",
                table: "ReportedDeath",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RejectedByGeneralAdmin",
                table: "ReportedDeath",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RejectedByRegionalAdmin",
                table: "ReportedDeath",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedByGeneralAdmin",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "ApprovedByRegionalAdmin",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "RejectedByGeneralAdmin",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "RejectedByRegionalAdmin",
                table: "ReportedDeath");
        }
    }
}
