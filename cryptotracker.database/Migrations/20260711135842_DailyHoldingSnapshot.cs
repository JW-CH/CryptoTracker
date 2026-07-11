using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cryptotracker.database.Migrations
{
    /// <inheritdoc />
    public partial class DailyHoldingSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetMeasurings");

            // deliberate fresh start (single-user decision, 2026-07-11): holdings are
            // rebuilt by the next import, and the price history still contained the
            // inverted fiat rows from before the bug-1 fix — wipe it as well
            migrationBuilder.Sql("DELETE FROM \"AssetPriceHistory\";");

            migrationBuilder.CreateTable(
                name: "DailyHoldings",
                columns: table => new
                {
                    IntegrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,10)", precision: 18, scale: 10, nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyHoldings", x => new { x.IntegrationId, x.Symbol, x.Date });
                    table.ForeignKey(
                        name: "FK_DailyHoldings_Assets_Symbol",
                        column: x => x.Symbol,
                        principalTable: "Assets",
                        principalColumn: "Symbol",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DailyHoldings_ExchangeIntegrations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "ExchangeIntegrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyHoldings_Symbol",
                table: "DailyHoldings",
                column: "Symbol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyHoldings");

            migrationBuilder.CreateTable(
                name: "AssetMeasurings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,10)", precision: 18, scale: 10, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetMeasurings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetMeasurings_Assets_Symbol",
                        column: x => x.Symbol,
                        principalTable: "Assets",
                        principalColumn: "Symbol",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetMeasurings_ExchangeIntegrations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "ExchangeIntegrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetMeasurings_IntegrationId",
                table: "AssetMeasurings",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetMeasurings_Symbol",
                table: "AssetMeasurings",
                column: "Symbol");
        }
    }
}
