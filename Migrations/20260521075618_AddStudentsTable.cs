using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcheryAlley.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StudentId",
                table: "ClassRegistrations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    StudentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentCustomerId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ICNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Birthday = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.StudentId);
                    table.ForeignKey(
                        name: "FK_Students_Customers_ParentCustomerId",
                        column: x => x.ParentCustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassRegistrations_StudentId",
                table: "ClassRegistrations",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_ParentCustomerId",
                table: "Students",
                column: "ParentCustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassRegistrations_Students_StudentId",
                table: "ClassRegistrations",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "StudentId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassRegistrations_Students_StudentId",
                table: "ClassRegistrations");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropIndex(
                name: "IX_ClassRegistrations_StudentId",
                table: "ClassRegistrations");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "ClassRegistrations");
        }
    }
}
