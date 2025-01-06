using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cryptotracker.database.Migrations
{
    /// <inheritdoc />
    public partial class AssetImagesNew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image",
                table: "AssetPriceHistory");

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "Assets",
                type: "longtext",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image",
                table: "Assets");

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "AssetPriceHistory",
                type: "longtext",
                nullable: false);
        }
    }
}
