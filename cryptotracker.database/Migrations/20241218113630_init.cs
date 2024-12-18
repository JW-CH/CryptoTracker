using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cryptotracker.database.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
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
                    AssetId = table.Column<string>(type: "varchar(255)", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.AssetId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExchangeIntegrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeIntegrations", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AssetMeasurings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    AssetId = table.Column<string>(type: "varchar(255)", nullable: true),
                    IntegrationId = table.Column<Guid>(type: "char(36)", nullable: false),
                    StandingDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StandingValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetMeasurings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetMeasurings_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "AssetId");
                    table.ForeignKey(
                        name: "FK_AssetMeasurings_ExchangeIntegrations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "ExchangeIntegrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AssetMeasurings_AssetId",
                table: "AssetMeasurings",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetMeasurings_IntegrationId",
                table: "AssetMeasurings",
                column: "IntegrationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetMeasurings");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "ExchangeIntegrations");
        }
    }
}
