using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaUploadFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaFiles_ProjectId",
                table: "MediaFiles");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "MediaFiles",
                newName: "StoragePath");

            migrationBuilder.AddColumn<string>(
                name: "Bucket",
                table: "MediaFiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Checksum",
                table: "MediaFiles",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Extension",
                table: "MediaFiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "MediaFiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "MediaFiles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "MediaFiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StoredFileName",
                table: "MediaFiles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UploadedAt",
                table: "MediaFiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "MediaFiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_ProjectId_UploadedAt",
                table: "MediaFiles",
                columns: new[] { "ProjectId", "UploadedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_StoragePath",
                table: "MediaFiles",
                column: "StoragePath",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaFiles_ProjectId_UploadedAt",
                table: "MediaFiles");

            migrationBuilder.DropIndex(
                name: "IX_MediaFiles_StoragePath",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "Bucket",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "Checksum",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "Extension",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "StoredFileName",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "UploadedAt",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "MediaFiles");

            migrationBuilder.RenameColumn(
                name: "StoragePath",
                table: "MediaFiles",
                newName: "Url");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_ProjectId",
                table: "MediaFiles",
                column: "ProjectId");
        }
    }
}
