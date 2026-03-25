using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotosStorageMap.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddArchiveItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ArchiveDownloadBytes",
                table: "UploadCollections",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "ArchiveDownloadCount",
                table: "UploadCollections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "ArchiveUploadBytes",
                table: "UploadCollections",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "ArchiveUploadCount",
                table: "UploadCollections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ArchiveItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadCollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchiveItems_UploadCollections_UploadCollectionId",
                        column: x => x.UploadCollectionId,
                        principalTable: "UploadCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveItems_StorageKey",
                table: "ArchiveItems",
                column: "StorageKey");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveItems_UploadCollectionId",
                table: "ArchiveItems",
                column: "UploadCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveItems_UploadCollectionId_CreatedAtUtc",
                table: "ArchiveItems",
                columns: new[] { "UploadCollectionId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchiveItems");

            migrationBuilder.DropColumn(
                name: "ArchiveDownloadBytes",
                table: "UploadCollections");

            migrationBuilder.DropColumn(
                name: "ArchiveDownloadCount",
                table: "UploadCollections");

            migrationBuilder.DropColumn(
                name: "ArchiveUploadBytes",
                table: "UploadCollections");

            migrationBuilder.DropColumn(
                name: "ArchiveUploadCount",
                table: "UploadCollections");
        }
    }
}
