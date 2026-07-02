using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotosStorageMap.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOriginalDeleteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginalDeleteError",
                table: "PhotoItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OriginalDeleteRequested",
                table: "PhotoItems",
                type: "boolean",
                maxLength: 2000,
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OriginalDeleteRequestedAtUtc",
                table: "PhotoItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OriginalDeletedAtUtc",
                table: "PhotoItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhotoItems_OriginalDeleteRequested_OriginalDeleteRequestedA~",
                table: "PhotoItems",
                columns: new[] { "OriginalDeleteRequested", "OriginalDeleteRequestedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PhotoItems_OriginalDeleteRequested_OriginalDeleteRequestedA~",
                table: "PhotoItems");

            migrationBuilder.DropColumn(
                name: "OriginalDeleteError",
                table: "PhotoItems");

            migrationBuilder.DropColumn(
                name: "OriginalDeleteRequested",
                table: "PhotoItems");

            migrationBuilder.DropColumn(
                name: "OriginalDeleteRequestedAtUtc",
                table: "PhotoItems");

            migrationBuilder.DropColumn(
                name: "OriginalDeletedAtUtc",
                table: "PhotoItems");
        }
    }
}
