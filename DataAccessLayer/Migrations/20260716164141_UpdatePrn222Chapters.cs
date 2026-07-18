using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePrn222Chapters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 1,
                column: "Title",
                value: "Chapter 01 - Networking Programming");

            migrationBuilder.UpdateData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 2,
                column: "Title",
                value: "Chapter 02 - Asynchronous and Parallel Programming in .NET");

            migrationBuilder.InsertData(
                table: "Chapters",
                columns: new[] { "Id", "IsDeleted", "OrderIndex", "SubjectId", "Title" },
                values: new object[,]
                {
                    { 4, false, 3, 1, "Chapter 03 - Dependency Injection in .NET" },
                    { 5, false, 4, 1, "Chapter 04 - Building Web Application using ASP.NET Core MVC" },
                    { 6, false, 5, 1, "Chapter 05 - Building Websites Using ASP.NET Core Razor Pages" },
                    { 7, false, 6, 1, "Chapter 06 - Building a Web App with Blazor and ASP.NET Core" },
                    { 8, false, 7, 1, "Chapter 07 - Real-Time Communication" },
                    { 9, false, 8, 1, "Chapter 08 - Background Tasks with Worker Service" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.UpdateData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 1,
                column: "Title",
                value: "Chương 1: .NET Core và C# Nâng cao");

            migrationBuilder.UpdateData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 2,
                column: "Title",
                value: "Chương 2: Entity Framework Core");
        }
    }
}
