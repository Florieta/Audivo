using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAudiobookGenreAndOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Genre",
                table: "Audiobooks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UploadedByUserId",
                table: "Audiobooks",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Audiobooks_UploadedByUserId",
                table: "Audiobooks",
                column: "UploadedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Audiobooks_AspNetUsers_UploadedByUserId",
                table: "Audiobooks",
                column: "UploadedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Audiobooks_AspNetUsers_UploadedByUserId",
                table: "Audiobooks");

            migrationBuilder.DropIndex(
                name: "IX_Audiobooks_UploadedByUserId",
                table: "Audiobooks");

            migrationBuilder.DropColumn(
                name: "Genre",
                table: "Audiobooks");

            migrationBuilder.DropColumn(
                name: "UploadedByUserId",
                table: "Audiobooks");
        }
    }
}
