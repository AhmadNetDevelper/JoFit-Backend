using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoFit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseSportLinkId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseSportLinks",
                columns: table => new
                {
                    CourseId = table.Column<long>(type: "bigint", nullable: false),
                    SportId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseSportLinks", x => new { x.CourseId, x.SportId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseSportLinks_SportId",
                table: "CourseSportLinks",
                column: "SportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseSportLinks");
        }
    }
}
