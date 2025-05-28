using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class AddeddeletedUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeletedUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DependentId = table.Column<int>(type: "int", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<int>(type: "int", nullable: false),
                    ApplicationStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SuccessorId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NoteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalDeclinerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovalDeclinerEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsConsent = table.Column<bool>(type: "bit", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SponsorsMemberName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SponsorsMemberNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SponsorLocalAdminName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SponsorLocalAdminNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PersonYearOfBirth = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PersonRegNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RegionId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsDeceased = table.Column<bool>(type: "bit", nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    OutwardPostcode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletedUser", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeletedUser");
        }
    }
}
