using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class addedStoreProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledStoredProcedure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcedureName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmailSubject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastRunDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledStoredProcedure", x => x.Id);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledStoredProcedure");

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "DateCreated",
                value: new DateTime(2025, 5, 8, 9, 5, 49, 651, DateTimeKind.Utc).AddTicks(202));

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "DateCreated",
                value: new DateTime(2025, 5, 8, 9, 5, 49, 651, DateTimeKind.Utc).AddTicks(204));

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "DateCreated",
                value: new DateTime(2025, 5, 8, 9, 5, 49, 651, DateTimeKind.Utc).AddTicks(205));

            migrationBuilder.UpdateData(
                table: "NoteTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "DateCreated",
                value: new DateTime(2025, 5, 8, 9, 5, 49, 651, DateTimeKind.Utc).AddTicks(206));
        }
    }
}
