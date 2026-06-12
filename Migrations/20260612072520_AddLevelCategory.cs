using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcheryAlley.Migrations
{
    /// <inheritdoc />
    public partial class AddLevelCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LevelCategory",
                table: "Archers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LevelCategory",
                table: "Archers");
        }
    }
}
