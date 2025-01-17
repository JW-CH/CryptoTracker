using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cryptotracker.database.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Symbol = table.Column<string>(type: "varchar(255)", nullable: false),
                    ExternalId = table.Column<string>(type: "longtext", nullable: true),
                    Name = table.Column<string>(type: "longtext", nullable: true),
                    Image = table.Column<string>(type: "longtext", nullable: true),
                    IsFiat = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsHidden = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Symbol);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExchangeIntegrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: true),
                    IsManual = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsHidden = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeIntegrations", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AssetPriceHistory",
                columns: table => new
                {
                    Symbol = table.Column<string>(type: "varchar(255)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Currency = table.Column<string>(type: "varchar(255)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,10)", precision: 18, scale: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetPriceHistory", x => new { x.Symbol, x.Date, x.Currency });
                    table.ForeignKey(
                        name: "FK_AssetPriceHistory_Assets_Symbol",
                        column: x => x.Symbol,
                        principalTable: "Assets",
                        principalColumn: "Symbol",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AssetMeasurings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Symbol = table.Column<string>(type: "varchar(255)", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,10)", precision: 18, scale: 10, nullable: false)
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
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AssetMeasurings_IntegrationId",
                table: "AssetMeasurings",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetMeasurings_Symbol",
                table: "AssetMeasurings",
                column: "Symbol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetMeasurings");

            migrationBuilder.DropTable(
                name: "AssetPriceHistory");

            migrationBuilder.DropTable(
                name: "ExchangeIntegrations");

            migrationBuilder.DropTable(
                name: "Assets");
        }
    }
}
