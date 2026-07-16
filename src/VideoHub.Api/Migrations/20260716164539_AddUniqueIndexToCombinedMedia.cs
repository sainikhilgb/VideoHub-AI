using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToCombinedMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CombinedMediaFiles_MediaFileId",
                table: "CombinedMediaFiles");

            migrationBuilder.CreateIndex(
                name: "IX_CombinedMediaFiles_MediaFileId_Language_MuxType",
                table: "CombinedMediaFiles",
                columns: new[] { "MediaFileId", "Language", "MuxType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CombinedMediaFiles_MediaFileId_Language_MuxType",
                table: "CombinedMediaFiles");

            migrationBuilder.CreateIndex(
                name: "IX_CombinedMediaFiles_MediaFileId",
                table: "CombinedMediaFiles",
                column: "MediaFileId");
        }
    }
}
