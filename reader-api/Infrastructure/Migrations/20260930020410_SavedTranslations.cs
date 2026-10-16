using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reader.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SavedTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TranslatedText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SourceLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TargetLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ContextText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MangaExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MangaTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChapterExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ChapterTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PageNumber = table.Column<int>(type: "integer", nullable: true),
                    SavedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedTranslations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedTranslations_UserId_SavedAt",
                table: "SavedTranslations",
                columns: new[] { "UserId", "SavedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedTranslations");
        }
    }
}
