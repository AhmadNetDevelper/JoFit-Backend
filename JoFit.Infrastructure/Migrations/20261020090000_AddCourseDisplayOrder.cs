using JoFit.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoFit.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(JoFitDbContext))]
    [Migration("20261020090000_AddCourseDisplayOrder")]
    public partial class AddCourseDisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Courses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Give existing courses a stable, distinct position (oldest first).
            migrationBuilder.Sql(
                "UPDATE \"Courses\" c SET \"DisplayOrder\" = r.rn FROM (SELECT \"Id\", ROW_NUMBER() OVER (ORDER BY \"Id\") - 1 AS rn FROM \"Courses\") r WHERE c.\"Id\" = r.\"Id\";");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_DisplayOrder_Id",
                table: "Courses",
                columns: new[] { "DisplayOrder", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_DisplayOrder_Id",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Courses");
        }
    }
}
