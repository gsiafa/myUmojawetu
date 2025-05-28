using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class addedcreatebytoNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "NoteHistory",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "DateCreated",
                value: new DateTime(2025, 2, 7, 10, 57, 58, 604, DateTimeKind.Utc).AddTicks(7654));

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "DateCreated",
                value: new DateTime(2025, 2, 7, 10, 57, 58, 604, DateTimeKind.Utc).AddTicks(7657));

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "DateCreated",
                value: new DateTime(2025, 2, 7, 10, 57, 58, 604, DateTimeKind.Utc).AddTicks(7657));

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "DateCreated",
                value: new DateTime(2025, 2, 7, 10, 57, 58, 604, DateTimeKind.Utc).AddTicks(7658));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "NoteHistory");

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "DateCreated",
                value: new DateTime(2025, 2, 7, 9, 12, 17, 482, DateTimeKind.Utc).AddTicks(4183));

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "DateCreated",
                value: new DateTime(2025, 2, 7, 9, 12, 17, 482, DateTimeKind.Utc).AddTicks(4185));

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "DateCreated",
                value: new DateTime(2025, 2, 7, 9, 12, 17, 482, DateTimeKind.Utc).AddTicks(4186));

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "DateCreated",
                value: new DateTime(2025, 2, 7, 9, 12, 17, 482, DateTimeKind.Utc).AddTicks(4187));
        }
    }
}
