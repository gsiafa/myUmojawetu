using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class UpDATEDChangeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DependantId",
                table: "ChangeLogs",
                newName: "NextOfKinId");

            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "ChangeLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateChanged",
                table: "ChangeLogs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DependentId",
                table: "ChangeLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FieldName",
                table: "ChangeLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Action",
                table: "ChangeLogs");

            migrationBuilder.DropColumn(
                name: "DateChanged",
                table: "ChangeLogs");

            migrationBuilder.DropColumn(
                name: "DependentId",
                table: "ChangeLogs");

            migrationBuilder.DropColumn(
                name: "FieldName",
                table: "ChangeLogs");

            migrationBuilder.RenameColumn(
                name: "NextOfKinId",
                table: "ChangeLogs",
                newName: "DependantId");
        }
    }
}
