using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangaReader.Migrations
{
    /// <inheritdoc />
    public partial class AddLevelingAndDecorations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create PfpDecorations table
            migrationBuilder.CreateTable(
                name: "PfpDecorations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImageUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LevelRequirement = table.Column<int>(type: "int", nullable: false),
                    IsAnimated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PfpDecorations", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Add new columns to Users
            migrationBuilder.AddColumn<int>(
                name: "XP",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EquippedDecorationId",
                table: "Users",
                type: "int",
                nullable: true);

            // Create ChapterViews table
            migrationBuilder.CreateTable(
                name: "ChapterViews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChapterViews_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChapterViews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Insert initial decoration
            migrationBuilder.InsertData(
                table: "PfpDecorations",
                columns: new[] { "Id", "CreatedAt", "ImageUrl", "IsAnimated", "LevelRequirement", "Name" },
                values: new object[] { 1, DateTime.UtcNow, "https://media.giphy.com/media/v1.Y2lkPTc5MGI3NjExNHJqZ3R4Z3R4Z3R4Z3R4Z3R4Z3R4Z3R4Z3R4Z3R4Z3R4JmVwPXYxX2ludGVybmFsX2dpZl9ieV9pZCZjdD1z/3o7TKMGV9mGfVf8kEw/giphy.gif", true, 1, "Glow Ring" });

            // Indices
            migrationBuilder.CreateIndex(
                name: "IX_ChapterViews_ChapterId",
                table: "ChapterViews",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterViews_UserId_ChapterId",
                table: "ChapterViews",
                columns: new[] { "UserId", "ChapterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EquippedDecorationId",
                table: "Users",
                column: "EquippedDecorationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_PfpDecorations_EquippedDecorationId",
                table: "Users",
                column: "EquippedDecorationId",
                principalTable: "PfpDecorations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_PfpDecorations_EquippedDecorationId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ChapterViews");

            migrationBuilder.DropTable(
                name: "PfpDecorations");

            migrationBuilder.DropColumn(
                name: "XP",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EquippedDecorationId",
                table: "Users");
        }
    }
}
