using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotosStorageMap.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUploads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StoragePlan",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "UploadCollections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PhotosPreviewCount = table.Column<int>(type: "integer", nullable: false),
                    PhotosDownloadCount = table.Column<int>(type: "integer", nullable: false),
                    MapPreviewCount = table.Column<int>(type: "integer", nullable: false),
                    TotalPhotos = table.Column<int>(type: "integer", nullable: false),
                    TotalBytes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadCollections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhotoItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadCollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StandardKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ThumbKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TakenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    StandardDeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhotoItems_UploadCollections_UploadCollectionId",
                        column: x => x.UploadCollectionId,
                        principalTable: "UploadCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShareLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadCollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    AllowDownload = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareLinks_UploadCollections_UploadCollectionId",
                        column: x => x.UploadCollectionId,
                        principalTable: "UploadCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhotoItems_UploadCollectionId",
                table: "PhotoItems",
                column: "UploadCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareLinks_Token",
                table: "ShareLinks",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShareLinks_UploadCollectionId",
                table: "ShareLinks",
                column: "UploadCollectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadCollections_OwnerUserId_CreatedAtUtc",
                table: "UploadCollections",
                columns: new[] { "OwnerUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UploadCollections_OwnerUserId_ExpiresAtUtc",
                table: "UploadCollections",
                columns: new[] { "OwnerUserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UploadCollections_OwnerUserId_TotalBytes",
                table: "UploadCollections",
                columns: new[] { "OwnerUserId", "TotalBytes" });

            migrationBuilder.CreateIndex(
                name: "IX_UploadCollections_OwnerUserId_TotalPhotos",
                table: "UploadCollections",
                columns: new[] { "OwnerUserId", "TotalPhotos" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhotoItems");

            migrationBuilder.DropTable(
                name: "ShareLinks");

            migrationBuilder.DropTable(
                name: "UploadCollections");

            migrationBuilder.DropColumn(
                name: "StoragePlan",
                table: "AspNetUsers");
        }
    }
}
