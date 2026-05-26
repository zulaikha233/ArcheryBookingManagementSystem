using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcheryAlley.Migrations
{
    /// <inheritdoc />
    public partial class AddArcherStatusAndReservationStudentFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StudentId",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Archers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "Pending");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_StudentId",
                table: "Reservations",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Archers_StudentId",
                table: "Reservations",
                column: "StudentId",
                principalTable: "Archers",
                principalColumn: "StudentId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Archers_StudentId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_StudentId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Archers");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "Reservations");
        }
    }
}
