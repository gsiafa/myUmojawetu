using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class AddedNonDeathTableAndUpdatedDependentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "Dependants",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "ISconverted",
                table: "Dependants",
                newName: "HasChangedFamily");

            migrationBuilder.AddColumn<Guid>(
                name: "OldUserId",
                table: "Dependants",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OldUserId",
                table: "Dependants");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Dependants",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "HasChangedFamily",
                table: "Dependants",
                newName: "ISconverted");
        }
    }
}
