using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetLanguagesToJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TargetLanguages",
                table: "Jobs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetLanguages",
                table: "Jobs");
        }
    }
}
