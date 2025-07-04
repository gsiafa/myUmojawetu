﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class removeGoodwillfromtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Goodwill",
                table: "Payment");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Goodwill",
                table: "Payment",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
