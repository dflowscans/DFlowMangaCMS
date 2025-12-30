using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangaReader.Migrations
{
    /// <inheritdoc />
    public partial class AddLockingAndHideReadingListExplicitJoin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "UserTitles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HideReadingList",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "PfpDecorations",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "UserUnlockedDecoration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DecorationId = table.Column<int>(type: "int", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserUnlockedDecoration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserUnlockedDecoration_PfpDecorations_DecorationId",
                        column: x => x.DecorationId,
                        principalTable: "PfpDecorations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserUnlockedDecoration_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserUnlockedTitle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TitleId = table.Column<int>(type: "int", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserUnlockedTitle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserUnlockedTitle_UserTitles_TitleId",
                        column: x => x.TitleId,
                        principalTable: "UserTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserUnlockedTitle_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "PfpDecorations",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsLocked",
                value: false);

            migrationBuilder.UpdateData(
                table: "PfpDecorations",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsLocked",
                value: false);

            migrationBuilder.UpdateData(
                table: "UserTitles",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsLocked",
                value: false);

            migrationBuilder.UpdateData(
                table: "UserTitles",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsLocked",
                value: false);

            migrationBuilder.UpdateData(
                table: "UserTitles",
                keyColumn: "Id",
                keyValue: 3,
                column: "IsLocked",
                value: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "HideReadingList",
                value: false);

            migrationBuilder.CreateIndex(
                name: "IX_UserUnlockedDecoration_DecorationId",
                table: "UserUnlockedDecoration",
                column: "DecorationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserUnlockedDecoration_UserId",
                table: "UserUnlockedDecoration",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserUnlockedTitle_TitleId",
                table: "UserUnlockedTitle",
                column: "TitleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserUnlockedTitle_UserId",
                table: "UserUnlockedTitle",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserUnlockedDecoration");

            migrationBuilder.DropTable(
                name: "UserUnlockedTitle");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "UserTitles");

            migrationBuilder.DropColumn(
                name: "HideReadingList",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "PfpDecorations");
        }
    }
}
