using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcheryAlley.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCustomerIC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Birthday",
                table: "Customers");

            migrationBuilder.AddColumn<string>(
                name: "ICNumber",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ICNumber",
                table: "Customers");

            migrationBuilder.AddColumn<DateTime>(
                name: "Birthday",
                table: "Customers",
                type: "datetime2",
                nullable: true);
        }
    }
}
