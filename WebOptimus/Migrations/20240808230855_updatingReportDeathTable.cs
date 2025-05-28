using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class updatingReportDeathTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "ReportedDeath",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBurial",
                table: "ReportedDeath",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "DeceasedPhotoPath",
                table: "ReportedDeath",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OtherRelevantInformation",
                table: "ReportedDeath",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlaceOfBurial",
                table: "ReportedDeath",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RegionId",
                table: "ReportedDeath",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CityId",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "DateOfBurial",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "DeceasedPhotoPath",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "OtherRelevantInformation",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "PlaceOfBurial",
                table: "ReportedDeath");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "ReportedDeath");
        }
    }
}
