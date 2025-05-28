using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class seperationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeparationHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OldUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DependentId = table.Column<int>(type: "int", nullable: false),
                    DependentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SeparationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SeparatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldUserFirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldUserSurname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldUserEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewUserFirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewUserSurname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewUserEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldNumberOfDependants = table.Column<int>(type: "int", nullable: true),
                    NewNumberOfDependants = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeparationHistory", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeparationHistory");
        }
    }
}
