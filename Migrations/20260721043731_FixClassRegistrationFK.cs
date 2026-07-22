using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcheryAlley.Migrations
{
    /// <inheritdoc />
    public partial class FixClassRegistrationFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ClassRegistrations_Students_StudentId')
                BEGIN
                    ALTER TABLE ClassRegistrations DROP CONSTRAINT FK_ClassRegistrations_Students_StudentId;
                    ALTER TABLE ClassRegistrations ADD CONSTRAINT FK_ClassRegistrations_Archers_StudentId FOREIGN KEY (StudentId) REFERENCES Archers(StudentId);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
