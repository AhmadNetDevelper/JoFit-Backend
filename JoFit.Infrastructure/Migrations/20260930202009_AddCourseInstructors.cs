using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoFit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseInstructors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseInstructorLinks",
                columns: table => new
                {
                    CourseId = table.Column<long>(type: "bigint", nullable: false),
                    CoachId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseInstructorLinks", x => new { x.CourseId, x.CoachId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseInstructorLinks_CoachId",
                table: "CourseInstructorLinks",
                column: "CoachId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseInstructorLinks");
        }
    }
}
