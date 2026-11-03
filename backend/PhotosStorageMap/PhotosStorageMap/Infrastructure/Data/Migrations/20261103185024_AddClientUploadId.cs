using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotosStorageMap.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClientUploadId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientUploadId",
                table: "PhotoItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhotoItems_ClientUploadId",
                table: "PhotoItems",
                column: "ClientUploadId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PhotoItems_ClientUploadId",
                table: "PhotoItems");

            migrationBuilder.DropColumn(
                name: "ClientUploadId",
                table: "PhotoItems");
        }
    }
}
