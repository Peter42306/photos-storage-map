using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotosStorageMap.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeletedToUploadCollectionCorrect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UploadCollections",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UploadCollections");
        }
    }
}
