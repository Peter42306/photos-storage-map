using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotosStorageMap.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ArchiveExportJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArchiveExportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FileCount = table.Column<int>(type: "integer", nullable: false),
                    TotalBytes = table.Column<long>(type: "bigint", nullable: false),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadyAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveExportJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchiveExportJobs_UploadCollections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "UploadCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveExportJobs_CollectionId",
                table: "ArchiveExportJobs",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveExportJobs_CollectionId_Type_Status",
                table: "ArchiveExportJobs",
                columns: new[] { "CollectionId", "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveExportJobs_ExpiresAtUtc",
                table: "ArchiveExportJobs",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveExportJobs_OwnerUserId_Status",
                table: "ArchiveExportJobs",
                columns: new[] { "OwnerUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveExportJobs_Status_CreatedAtUtc",
                table: "ArchiveExportJobs",
                columns: new[] { "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchiveExportJobs");
        }
    }
}
