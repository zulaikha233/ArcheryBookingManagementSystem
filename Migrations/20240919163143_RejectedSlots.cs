using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcheryAlley.Migrations
{
    /// <inheritdoc />
    public partial class RejectedSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RejectedSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservationId = table.Column<int>(type: "int", nullable: false),
                    ReservedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SlotId = table.Column<int>(type: "int", nullable: false),
                    SlotStartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    SlotEndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EmpName = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    EmpId = table.Column<int>(type: "int", nullable: false)
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
                name: "IX_ReservationRejections_EmpId",
                table: "ReservationRejections",
                column: "EmpId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RejectedSlots");

            migrationBuilder.DropTable(
                name: "ReservationRejections");
        }
    }
}
