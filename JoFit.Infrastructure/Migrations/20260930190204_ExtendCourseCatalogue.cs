using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JoFit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendCourseCatalogue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CertificationIssuedByAr",
                table: "Courses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificationIssuedByEn",
                table: "Courses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificationNameAr",
                table: "Courses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificationNameEn",
                table: "Courses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CompareAtAmount",
                table: "Courses",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionAr",
                table: "Courses",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "Courses",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DurationHours",
                table: "Courses",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Format",
                table: "Courses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "online");

            migrationBuilder.AddColumn<int>(
                name: "IntakeSeatsTotal",
                table: "Courses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "IntakeStartsOn",
                table: "Courses",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Languages",
                table: "Courses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "ar");

            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "Courses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "beginner");

            migrationBuilder.AddColumn<int>(
                name: "ModuleCount",
                table: "Courses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "PriceAmount",
                table: "Courses",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishedAtUtc",
                table: "Courses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Courses",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Courses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "draft");

            migrationBuilder.AddColumn<string>(
                name: "SummaryAr",
                table: "Courses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SummaryEn",
                table: "Courses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VenueAr",
                table: "Courses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VenueEn",
                table: "Courses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CourseCategoryLinks",
                columns: table => new
                {
                    CourseId = table.Column<long>(type: "bigint", nullable: false),
                    CourseCategoryId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseCategoryLinks", x => new { x.CourseId, x.CourseCategoryId });
                });

            migrationBuilder.CreateTable(
                name: "RecordMedia",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OwnerId = table.Column<long>(type: "bigint", nullable: false),
                    Field = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    MediaId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Alt = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordMedia", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Slug",
                table: "Courses",
                column: "Slug",
                unique: true,
                filter: "\"Slug\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CourseCategoryLinks_CourseCategoryId",
                table: "CourseCategoryLinks",
                column: "CourseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordMedia_OwnerType_OwnerId_Field",
                table: "RecordMedia",
                columns: new[] { "OwnerType", "OwnerId", "Field" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseCategoryLinks");

            migrationBuilder.DropTable(
                name: "RecordMedia");

            migrationBuilder.DropIndex(
                name: "IX_Courses_Slug",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CertificationIssuedByAr",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CertificationIssuedByEn",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CertificationNameAr",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CertificationNameEn",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CompareAtAmount",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DescriptionAr",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DurationHours",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Format",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "IntakeSeatsTotal",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "IntakeStartsOn",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Languages",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "ModuleCount",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "PriceAmount",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "PublishedAtUtc",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "SummaryAr",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "SummaryEn",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "VenueAr",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "VenueEn",
                table: "Courses");
        }
    }
}
