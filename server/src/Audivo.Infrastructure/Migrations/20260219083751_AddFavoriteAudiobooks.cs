using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoriteAudiobooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FavoriteAudiobooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AudiobookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteAudiobooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FavoriteAudiobooks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FavoriteAudiobooks_Audiobooks_AudiobookId",
                        column: x => x.AudiobookId,
                        principalTable: "Audiobooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteAudiobooks_AudiobookId",
                table: "FavoriteAudiobooks",
                column: "AudiobookId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteAudiobooks_UserId_AudiobookId",
                table: "FavoriteAudiobooks",
                columns: new[] { "UserId", "AudiobookId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoriteAudiobooks");
        }
    }
}
