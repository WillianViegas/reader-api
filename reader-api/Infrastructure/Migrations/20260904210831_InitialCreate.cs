using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reader.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LibraryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MangaProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MangaExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MangaTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MangaCoverUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    MangaOriginalLanguage = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MangaSnapshotUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalSubject = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReadingProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChapterExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChapterLanguage = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChapterTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChapterVolume = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ChapterNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CurrentPage = table.Column<int>(type: "integer", nullable: false),
                    PageCount = table.Column<int>(type: "integer", nullable: true),
                    LastReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LibraryItemId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReadingProgresses_LibraryItems_LibraryItemId",
                        column: x => x.LibraryItemId,
                        principalTable: "LibraryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryItems_UserId",
                table: "LibraryItems",
                column: "UserId");

            // Composite indexes spanning owner and owned-type columns; not expressible in the EF model.
            migrationBuilder.CreateIndex(
                name: "IX_LibraryItems_UserId_MangaProvider_MangaExternalId",
                table: "LibraryItems",
                columns: new[] { "UserId", "MangaProvider", "MangaExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryItems_MangaProvider_MangaExternalId",
                table: "LibraryItems",
                columns: new[] { "MangaProvider", "MangaExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgresses_ChapterProvider_ChapterExternalId",
                table: "ReadingProgresses",
                columns: new[] { "ChapterProvider", "ChapterExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgresses_LibraryItemId",
                table: "ReadingProgresses",
                column: "LibraryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalSubject",
                table: "Users",
                column: "ExternalSubject",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReadingProgresses");
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "LibraryItems");
        }
    }
}
