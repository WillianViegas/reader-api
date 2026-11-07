using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reader.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ArchiveLibraryItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "LibraryItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "LibraryItems");
        }
    }
}
