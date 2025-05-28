using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class addedextrafieldToUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdateOn",
                table: "Cause");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeceased",
                table: "AspNetUsers",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeceased",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateOn",
                table: "Cause",
                type: "datetime2",
                nullable: true);
        }
    }
}
