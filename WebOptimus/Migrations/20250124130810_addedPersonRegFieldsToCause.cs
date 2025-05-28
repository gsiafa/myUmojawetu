using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class addedPersonRegFieldsToCause : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountRaised",
                table: "Cause");

            migrationBuilder.AddColumn<string>(
                name: "PersonRegNumber",
                table: "Cause",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonRegNumber",
                table: "Cause");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountRaised",
                table: "Cause",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
