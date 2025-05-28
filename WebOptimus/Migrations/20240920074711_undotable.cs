using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class undotable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentSessionDependent");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                table: "PaymentSessions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "PaymentSessions");

            migrationBuilder.CreateTable(
                name: "PaymentSessionDependent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DependentId = table.Column<int>(type: "int", nullable: false),
                    PaymentSessionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSessionDependent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentSessionDependent_Dependants_DependentId",
                        column: x => x.DependentId,
                        principalTable: "Dependants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentSessionDependent_PaymentSessions_PaymentSessionId",
                        column: x => x.PaymentSessionId,
                        principalTable: "PaymentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSessionDependent_DependentId",
                table: "PaymentSessionDependent",
                column: "DependentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSessionDependent_PaymentSessionId",
                table: "PaymentSessionDependent",
                column: "PaymentSessionId");
        }
    }
}
