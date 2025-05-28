using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class addedExtraColumnsToDependentsANDUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Dependants",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegionId",
                table: "Dependants",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLogin",
                table: "AspNetUsers",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Dependants");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "Dependants");

            migrationBuilder.DropColumn(
                name: "IsLogin",
                table: "AspNetUsers");
        }
    }
}
