using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcheryAlley.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRejectedSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropForeignKey(
            //     name: "FK_Reservations_Roles",
            //     table: "Reservations");

            migrationBuilder.DropTable(
                name: "RejectedSlots");

            migrationBuilder.DropTable(
                name: "ReservationRejections");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_ReservedBy",
                table: "Reservations");

            migrationBuilder.AlterColumn<string>(
                name: "ReservedBy",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ReservedBy",
                table: "Reservations",
                type: "nvarchar(15)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "RejectedSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReservationId = table.Column<int>(type: "int", nullable: false),
                    ReservedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SlotEndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    SlotId = table.Column<int>(type: "int", nullable: false),
                    SlotStartTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RejectedSlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReservationRejections",
                columns: table => new
                {
                    ReservationId = table.Column<int>(type: "int", nullable: false),
                    EmpId = table.Column<string>(type: "nvarchar(15)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationRejections", x => new { x.ReservationId, x.EmpId });
                    table.ForeignKey(
                        name: "FK_ReservationRejections_Reservations",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "ReservationId");
                    table.ForeignKey(
                        name: "FK_ReservationRejections_Roles",
                        column: x => x.EmpId,
                        principalTable: "Roles",
                        principalColumn: "EmpId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ReservedBy",
                table: "Reservations",
                column: "ReservedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRejections_EmpId",
                table: "ReservationRejections",
                column: "EmpId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Roles",
                table: "Reservations",
                column: "ReservedBy",
                principalTable: "Roles",
                principalColumn: "EmpId");
        }
    }
}
