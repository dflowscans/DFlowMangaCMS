using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangaReader.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExistingOriginsToLevelUnlock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE UserUnlockedTitle SET Origin = 1"); // 1 is LevelUnlock
            migrationBuilder.Sql("UPDATE UserUnlockedDecoration SET Origin = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
