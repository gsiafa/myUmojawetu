using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class addedGoodWill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberOfDependants",
                table: "Dependants");

            migrationBuilder.AddColumn<decimal>(
                name: "GoodwillAmount",
                table: "Payment",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoodwillAmount",
                table: "Payment");

            migrationBuilder.AddColumn<int>(
                name: "NumberOfDependants",
                table: "Dependants",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
