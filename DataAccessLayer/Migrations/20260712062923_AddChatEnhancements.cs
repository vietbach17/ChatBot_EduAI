using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddChatEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubjectId",
                table: "ChatSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ChatSessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "MessageType",
                table: "ChatMessages",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TokenCount",
                table: "ChatMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_SubjectId",
                table: "ChatSessions",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatSessions_Subjects_SubjectId",
                table: "ChatSessions",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatSessions_Subjects_SubjectId",
                table: "ChatSessions");

            migrationBuilder.DropIndex(
                name: "IX_ChatSessions_SubjectId",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "TokenCount",
                table: "ChatMessages");
        }
    }
}
