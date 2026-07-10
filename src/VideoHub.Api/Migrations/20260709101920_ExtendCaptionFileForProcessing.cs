using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExtendCaptionFileForProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CaptionFiles_TranscriptOrTranslation",
                table: "CaptionFiles");

            migrationBuilder.AddColumn<string>(
                name: "StatusMessage",
                table: "Jobs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BlobUrl",
                table: "CaptionFiles",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "CaptionFiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "JobId",
                table: "CaptionFiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CaptionFiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CaptionFiles_JobId",
                table: "CaptionFiles",
                column: "JobId");

            migrationBuilder.AddForeignKey(
                name: "FK_CaptionFiles_Jobs_JobId",
                table: "CaptionFiles",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CaptionFiles_Jobs_JobId",
                table: "CaptionFiles");

            migrationBuilder.DropIndex(
                name: "IX_CaptionFiles_JobId",
                table: "CaptionFiles");

            migrationBuilder.DropColumn(
                name: "StatusMessage",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "CaptionFiles");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "CaptionFiles");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CaptionFiles");

            migrationBuilder.AlterColumn<string>(
                name: "BlobUrl",
                table: "CaptionFiles",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CaptionFiles_TranscriptOrTranslation",
                table: "CaptionFiles",
                sql: "\"TranscriptId\" IS NOT NULL OR \"TranslationId\" IS NOT NULL");
        }
    }
}
