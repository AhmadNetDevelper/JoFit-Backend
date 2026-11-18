using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoFit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CoachJobApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "CustomerId",
                table: "JobApplications",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "CoachId",
                table: "JobApplications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CustomerId",
                table: "CvFiles",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "CoachId",
                table: "CvFiles",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_CoachId",
                table: "JobApplications",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_JobId_CoachId",
                table: "JobApplications",
                columns: new[] { "JobId", "CoachId" },
                unique: true,
                filter: "\"Status\" <> 'withdrawn' AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CvFiles_CoachId",
                table: "CvFiles",
                column: "CoachId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobApplications_CoachId",
                table: "JobApplications");

            migrationBuilder.DropIndex(
                name: "IX_JobApplications_JobId_CoachId",
                table: "JobApplications");

            migrationBuilder.DropIndex(
                name: "IX_CvFiles_CoachId",
                table: "CvFiles");

            migrationBuilder.DropColumn(
                name: "CoachId",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "CoachId",
                table: "CvFiles");

            migrationBuilder.AlterColumn<long>(
                name: "CustomerId",
                table: "JobApplications",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CustomerId",
                table: "CvFiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
