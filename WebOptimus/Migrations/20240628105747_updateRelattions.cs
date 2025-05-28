using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class updateRelattions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DependentChecklistItems_PaymentSessions_PaymentSessionId",
                table: "DependentChecklistItems");

            migrationBuilder.AlterColumn<int>(
                name: "PaymentSessionId",
                table: "DependentChecklistItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DependentChecklistItems_PaymentSessions_PaymentSessionId",
                table: "DependentChecklistItems",
                column: "PaymentSessionId",
                principalTable: "PaymentSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DependentChecklistItems_PaymentSessions_PaymentSessionId",
                table: "DependentChecklistItems");

            migrationBuilder.AlterColumn<int>(
                name: "PaymentSessionId",
                table: "DependentChecklistItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_DependentChecklistItems_PaymentSessions_PaymentSessionId",
                table: "DependentChecklistItems",
                column: "PaymentSessionId",
                principalTable: "PaymentSessions",
                principalColumn: "Id");
        }
    }
}
