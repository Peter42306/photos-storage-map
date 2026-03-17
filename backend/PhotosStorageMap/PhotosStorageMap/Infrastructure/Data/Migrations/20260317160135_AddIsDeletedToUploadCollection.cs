using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotosStorageMap.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeletedToUploadCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PhotoItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PhotoItems");
        }
    }
}
