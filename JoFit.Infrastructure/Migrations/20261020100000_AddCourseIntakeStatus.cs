using JoFit.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoFit.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(JoFitDbContext))]
    [Migration("20261020100000_AddCourseIntakeStatus")]
    public partial class AddCourseIntakeStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing courses get "auto", so nothing changes for them.
            migrationBuilder.AddColumn<string>(
                name: "IntakeStatus",
                table: "Courses",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "auto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntakeStatus",
                table: "Courses");
        }
    }
}
