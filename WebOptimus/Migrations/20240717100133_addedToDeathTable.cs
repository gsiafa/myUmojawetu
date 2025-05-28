using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class addedToDeathTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateJoined",
                table: "ReportedDeath",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "DeceasedName",
                table: "ReportedDeath",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeceasedNextOfKinEmail",
                table: "ReportedDeath",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeceasedNextOfKinName",
                table: "ReportedDeath",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeceasedNextOfKinPhoneNumber",
                table: "ReportedDeath",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeceasedNextOfKinRelationship",
                table: "ReportedDeath",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeceasedRegNumber",
                table: "ReportedDeath",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeceasedYearOfBirth",
                table: "ReportedDeath",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateJoined",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "DeceasedName",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "DeceasedNextOfKinEmail",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "DeceasedNextOfKinName",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "DeceasedNextOfKinPhoneNumber",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "DeceasedNextOfKinRelationship",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "DeceasedRegNumber",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "DeceasedYearOfBirth",
                table: "ReportedDeath");
        }
    }
}
