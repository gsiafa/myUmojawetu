using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class updatescheduleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DayOfMonth",
                table: "ScheduledStoredProcedure",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Frequency",
                table: "ScheduledStoredProcedure",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRunNow",
                table: "ScheduledStoredProcedure",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "DateCreated",
                value: new DateTime(2025, 6, 13, 7, 40, 56, 770, DateTimeKind.Utc).AddTicks(265));

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "DateCreated",
                value: new DateTime(2025, 6, 13, 7, 40, 56, 770, DateTimeKind.Utc).AddTicks(267));

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "DateCreated",
                value: new DateTime(2025, 6, 13, 7, 40, 56, 770, DateTimeKind.Utc).AddTicks(268));

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "DateCreated",
                value: new DateTime(2025, 6, 13, 7, 40, 56, 770, DateTimeKind.Utc).AddTicks(269));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DayOfMonth",
                table: "ScheduledStoredProcedure");

            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "ScheduledStoredProcedure");

            migrationBuilder.DropColumn(
                name: "IsRunNow",
                table: "ScheduledStoredProcedure");

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "DateCreated",
                value: new DateTime(2025, 6, 12, 17, 33, 21, 629, DateTimeKind.Utc).AddTicks(9419));

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "DateCreated",
                value: new DateTime(2025, 6, 12, 17, 33, 21, 629, DateTimeKind.Utc).AddTicks(9456));

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "DateCreated",
                value: new DateTime(2025, 6, 12, 17, 33, 21, 629, DateTimeKind.Utc).AddTicks(9457));

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "DateCreated",
                value: new DateTime(2025, 6, 12, 17, 33, 21, 629, DateTimeKind.Utc).AddTicks(9458));
        }
    }
}
