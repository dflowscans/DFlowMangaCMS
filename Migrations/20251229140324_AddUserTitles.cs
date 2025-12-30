using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MangaReader.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTitles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EquippedTitleId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserTitles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Color = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LevelRequirement = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTitles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "UserTitles",
                columns: new[] { "Id", "Color", "CreatedAt", "LevelRequirement", "Name" },
                values: new object[,]
                {
                    { 1, "#94a3b8", new DateTime(2025, 12, 29, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Novice Reader" },
                    { 2, "#3b82f6", new DateTime(2025, 12, 29, 0, 0, 0, 0, DateTimeKind.Utc), 5, "Manga Enthusiast" },
                    { 3, "#f59e0b", new DateTime(2025, 12, 29, 0, 0, 0, 0, DateTimeKind.Utc), 10, "Legendary Scholar" }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "EquippedTitleId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EquippedTitleId",
                table: "Users",
                column: "EquippedTitleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_UserTitles_EquippedTitleId",
                table: "Users",
                column: "EquippedTitleId",
                principalTable: "UserTitles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_UserTitles_EquippedTitleId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "UserTitles");

            migrationBuilder.DropIndex(
                name: "IX_Users_EquippedTitleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EquippedTitleId",
                table: "Users");
        }
    }
}
