using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebOptimus.Migrations
{
    /// <inheritdoc />
    public partial class UserIdTOpaymentSession3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "PaymentSessions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        




         
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Candidates");

            migrationBuilder.DropTable(
                name: "Gender");

            migrationBuilder.DropTable(
                name: "PopUpNotification");

            migrationBuilder.DropTable(
                name: "SeparationHistory");

            migrationBuilder.DropTable(
                name: "Successors");

            migrationBuilder.DropTable(
                name: "UploadedDocuments");

            migrationBuilder.DropTable(
                name: "UserNotification");

            migrationBuilder.DropTable(
                name: "Votes");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PaymentSessions");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Dependants");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Dependants");

            migrationBuilder.DropColumn(
                name: "Telephone",
                table: "Dependants");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Dependants");

            migrationBuilder.DropColumn(
                name: "SuccessorId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsActiveToInternMember",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "IsActiveToPublic",
                table: "Announcements");

        
        }
    }
}
