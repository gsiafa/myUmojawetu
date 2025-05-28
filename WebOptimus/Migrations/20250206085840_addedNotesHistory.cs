using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class addedNotesHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAccountActive",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "NoteTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NoteHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NoteTypeId = table.Column<int>(type: "int", nullable: false),
                    PersonRegNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoteHistory_NoteTypes_NoteTypeId",
                        column: x => x.NoteTypeId,
                        principalTable: "NoteTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "NoteTypes",
                columns: new[] { "Id", "DateCreated", "TypeName" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 2, 6, 8, 58, 38, 222, DateTimeKind.Utc).AddTicks(9258), "Account" },
                    { 2, new DateTime(2025, 2, 6, 8, 58, 38, 222, DateTimeKind.Utc).AddTicks(9260), "Complaints" },
                    { 3, new DateTime(2025, 2, 6, 8, 58, 38, 222, DateTimeKind.Utc).AddTicks(9261), "Payments" },
                    { 4, new DateTime(2025, 2, 6, 8, 58, 38, 222, DateTimeKind.Utc).AddTicks(9262), "General" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_NoteHistory_NoteTypeId",
                table: "NoteHistory",
                column: "NoteTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NoteHistory");

            migrationBuilder.DropTable(
                name: "NoteTypes");

            migrationBuilder.AddColumn<bool>(
                name: "IsAccountActive",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
