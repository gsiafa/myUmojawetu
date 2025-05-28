using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFieldInCaused : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountRaised",
                table: "DonationForNonDeathRelated");

            migrationBuilder.DropColumn(
                name: "IsDeathRelated",
                table: "Cause");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountRaised",
                table: "DonationForNonDeathRelated",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeathRelated",
                table: "Cause",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
