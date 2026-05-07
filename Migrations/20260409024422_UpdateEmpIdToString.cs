using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcheryAlley.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmpIdToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop constraints first
            migrationBuilder.DropForeignKey(name: "FK_Reservations_Roles", table: "Reservations");
            migrationBuilder.DropForeignKey(name: "FK_ReservationRejections_Roles", table: "ReservationRejections");
            migrationBuilder.DropForeignKey(name: "FK_ReservationRejections_Reservations", table: "ReservationRejections");
            
            migrationBuilder.DropPrimaryKey(name: "PK_Roles", table: "Roles");
            migrationBuilder.DropPrimaryKey(name: "PK_ReservationRejections", table: "ReservationRejections");

            // 2. Drop and recreate EmpId in Roles (to remove IDENTITY)
            migrationBuilder.DropColumn(name: "EmpId", table: "Roles");
            migrationBuilder.AddColumn<string>(
                name: "EmpId",
                table: "Roles",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false);
            
            migrationBuilder.AddPrimaryKey(name: "PK_Roles", table: "Roles", column: "EmpId");

            // 3. Update FK columns in other tables
            migrationBuilder.AlterColumn<string>(
                name: "ReservedBy",
                table: "Reservations",
                type: "nvarchar(15)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "EmpId",
                table: "ReservationRejections",
                type: "nvarchar(15)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            // 4. Re-add Primary Keys 
            migrationBuilder.AddPrimaryKey(name: "PK_ReservationRejections", table: "ReservationRejections", columns: new[] { "ReservationId", "EmpId" });

            // 5. Re-add Foreign Keys
            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Roles",
                table: "Reservations",
                column: "ReservedBy",
                principalTable: "Roles",
                principalColumn: "EmpId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationRejections_Roles",
                table: "ReservationRejections",
                column: "EmpId",
                principalTable: "Roles",
                principalColumn: "EmpId");
            
            migrationBuilder.AddForeignKey(
                name: "FK_ReservationRejections_Reservations",
                table: "ReservationRejections",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "ReservationId");

            // 6. Add any other columns
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "BookingSlots",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "BookingSlots");

            migrationBuilder.AlterColumn<int>(
                name: "EmpId",
                table: "Roles",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "ReservedBy",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EmpId",
                table: "ReservationRejections",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)");
        }
    }
}
