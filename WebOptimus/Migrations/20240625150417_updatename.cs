using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class updatename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeath",
                table: "Dependants");

            migrationBuilder.AddColumn<bool>(
                name: "IsDead",
                table: "Dependants",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDead",
                table: "Dependants");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeath",
                table: "Dependants",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
