using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotosStorageMap.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "PhotoItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalKey",
                table: "PhotoItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PhotoItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Error",
                table: "PhotoItems");

            migrationBuilder.DropColumn(
                name: "OriginalKey",
                table: "PhotoItems");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PhotoItems");
        }
    }
}
