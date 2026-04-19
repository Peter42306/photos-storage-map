using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotosStorageMap.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShareLinkPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AllowDownload",
                table: "ShareLinks",
                newName: "AllowSlideshowOriginals");

            migrationBuilder.AddColumn<bool>(
                name: "AllowDownloadOriginalFromCard",
                table: "ShareLinks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowDownloadResizedZip",
                table: "ShareLinks",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowDownloadOriginalFromCard",
                table: "ShareLinks");

            migrationBuilder.DropColumn(
                name: "AllowDownloadResizedZip",
                table: "ShareLinks");

            migrationBuilder.RenameColumn(
                name: "AllowSlideshowOriginals",
                table: "ShareLinks",
                newName: "AllowDownload");
        }
    }
}
