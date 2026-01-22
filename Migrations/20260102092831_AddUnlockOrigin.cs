using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangaReader.Migrations
{
    /// <inheritdoc />
    public partial class AddUnlockOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Origin",
                table: "UserUnlockedTitle",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Origin",
                table: "UserUnlockedDecoration",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Origin",
                table: "UserUnlockedTitle");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "UserUnlockedDecoration");
        }
    }
}
