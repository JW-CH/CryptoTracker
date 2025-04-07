using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cryptotracker.database.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIsFiatWithAssetType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssetType",
                table: "Assets",
                type: "longtext",
                nullable: false);

            // Werte aus IsFiat übernehmen
            migrationBuilder.Sql(
                "UPDATE Assets SET AssetType = 'Fiat' WHERE IsFiat = 1;" +
                "UPDATE Assets SET AssetType = 'Crypto' WHERE IsFiat = 0;"
            );

            migrationBuilder.DropColumn(
                name: "IsFiat",
                table: "Assets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFiat",
                table: "Assets",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            // Enum zurück in IsFiat konvertieren
            migrationBuilder.Sql(
                "UPDATE Assets SET IsFiat = 1 WHERE AssetType = 'Fiat';" +
                "UPDATE Assets SET IsFiat = 0 WHERE AssetType != 'Fiat';"
            );

            migrationBuilder.DropColumn(
                name: "AssetType",
                table: "Assets");
        }
    }
}
