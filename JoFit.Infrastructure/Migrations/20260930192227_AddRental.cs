using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JoFit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRental : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RentalBookings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RentalItemId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: true),
                    OrderId = table.Column<long>(type: "bigint", nullable: true),
                    From = table.Column<DateOnly>(type: "date", nullable: false),
                    To = table.Column<DateOnly>(type: "date", nullable: false),
                    Unit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Units = table.Column<int>(type: "integer", nullable: false),
                    UnitPriceAmount = table.Column<long>(type: "bigint", nullable: false),
                    SubtotalAmount = table.Column<long>(type: "bigint", nullable: false),
                    DepositAmount = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DepositReleasedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalBookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RentalItemCategoryLinks",
                columns: table => new
                {
                    RentalItemId = table.Column<long>(type: "bigint", nullable: false),
                    RentalCategoryId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalItemCategoryLinks", x => new { x.RentalItemId, x.RentalCategoryId });
                });

            migrationBuilder.CreateTable(
                name: "RentalItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slug = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TitleEn = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TitleAr = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SummaryEn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SummaryAr = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DescriptionEn = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    DescriptionAr = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    Condition = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DepotId = table.Column<long>(type: "bigint", nullable: false),
                    TotalUnits = table.Column<int>(type: "integer", nullable: false),
                    UnitsOutOfService = table.Column<int>(type: "integer", nullable: false),
                    DepositAmount = table.Column<long>(type: "bigint", nullable: true),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RentalRates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RentalItemId = table.Column<long>(type: "bigint", nullable: false),
                    Unit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PriceAmount = table.Column<long>(type: "bigint", nullable: false),
                    MinUnits = table.Column<int>(type: "integer", nullable: false),
                    MaxUnits = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentalRates_RentalItems_RentalItemId",
                        column: x => x.RentalItemId,
                        principalTable: "RentalItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RentalBookings_CustomerId",
                table: "RentalBookings",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalBookings_RentalItemId_From_To",
                table: "RentalBookings",
                columns: new[] { "RentalItemId", "From", "To" });

            migrationBuilder.CreateIndex(
                name: "IX_RentalBookings_Status",
                table: "RentalBookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RentalItemCategoryLinks_RentalCategoryId",
                table: "RentalItemCategoryLinks",
                column: "RentalCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalItems_DepotId",
                table: "RentalItems",
                column: "DepotId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalItems_Slug",
                table: "RentalItems",
                column: "Slug",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_RentalRates_RentalItemId_Unit",
                table: "RentalRates",
                columns: new[] { "RentalItemId", "Unit" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RentalBookings");

            migrationBuilder.DropTable(
                name: "RentalItemCategoryLinks");

            migrationBuilder.DropTable(
                name: "RentalRates");

            migrationBuilder.DropTable(
                name: "RentalItems");
        }
    }
}
