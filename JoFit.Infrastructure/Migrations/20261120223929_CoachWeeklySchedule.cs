using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoFit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CoachWeeklySchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WeeklySchedule",
                table: "Coaches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Carry every coach's existing hours over: "0,1,2,3,4" + 9..17 becomes "0:9-17,1:9-17,...".
            migrationBuilder.Sql("""
                UPDATE "Coaches"
                SET "WeeklySchedule" = COALESCE(
                    (SELECT string_agg(d || ':' || "DayStartHour" || '-' || "DayEndHour", ',' ORDER BY d::int)
                     FROM unnest(string_to_array("WorkingDays", ',')) AS d
                     WHERE d <> ''),
                    '0:9-17,1:9-17,2:9-17,3:9-17,4:9-17');
                """);

            migrationBuilder.DropColumn(
                name: "DayEndHour",
                table: "Coaches");

            migrationBuilder.DropColumn(
                name: "DayStartHour",
                table: "Coaches");

            migrationBuilder.DropColumn(
                name: "WorkingDays",
                table: "Coaches");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DayEndHour",
                table: "Coaches",
                type: "integer",
                nullable: false,
                defaultValue: 17);

            migrationBuilder.AddColumn<int>(
                name: "DayStartHour",
                table: "Coaches",
                type: "integer",
                nullable: false,
                defaultValue: 9);

            migrationBuilder.AddColumn<string>(
                name: "WorkingDays",
                table: "Coaches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "0,1,2,3,4");

            // Best effort: a single window cannot hold per-day hours, so keep the days and the first day's hours.
            migrationBuilder.Sql("""
                UPDATE "Coaches"
                SET "WorkingDays" = COALESCE((SELECT string_agg(split_part(e, ':', 1), ',') FROM unnest(string_to_array("WeeklySchedule", ',')) AS e), '0,1,2,3,4'),
                    "DayStartHour" = COALESCE(NULLIF(split_part(split_part(split_part("WeeklySchedule", ',', 1), ':', 2), '-', 1), ''), '9')::int,
                    "DayEndHour" = COALESCE(NULLIF(split_part(split_part(split_part("WeeklySchedule", ',', 1), ':', 2), '-', 2), ''), '17')::int;
                """);

            migrationBuilder.DropColumn(
                name: "WeeklySchedule",
                table: "Coaches");
        }
    }
}
