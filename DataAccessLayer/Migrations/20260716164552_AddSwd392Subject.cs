using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddSwd392Subject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Subjects",
                columns: new[] { "Id", "Code", "IsDeleted", "LecturerId", "Name" },
                values: new object[] { 3, "SWD392", false, 2, "Software Architecture and Design" });

            migrationBuilder.InsertData(
                table: "Chapters",
                columns: new[] { "Id", "IsDeleted", "OrderIndex", "SubjectId", "Title" },
                values: new object[,]
                {
                    { 10, false, 1, 3, "Ch01 - Introduction" },
                    { 11, false, 2, 3, "Ch02 - Overview of UML Notation" },
                    { 12, false, 3, 3, "Ch03 - Software Life Cycle Models and Processes" },
                    { 13, false, 4, 3, "Ch04 - Software Design and Architecture Concepts" },
                    { 14, false, 5, 3, "Ch05 - Overview of Software Modeling and Design" },
                    { 15, false, 6, 3, "Ch06 - Use Case Modeling" },
                    { 16, false, 7, 3, "Ch07 - Static Modeling" },
                    { 17, false, 8, 3, "Ch08 - Object and Class Structuring" },
                    { 18, false, 9, 3, "Ch09-11 - Dynamic Modeling" },
                    { 19, false, 10, 3, "Ch12 - Overview of Software Architecture" },
                    { 20, false, 11, 3, "Ch13 - Software Subsystem Architectural Design" },
                    { 21, false, 12, 3, "Ch14 - Designing Object-Oriented Software Architecture" },
                    { 22, false, 13, 3, "Ch15 - Designing Client Server Software Architecture" },
                    { 23, false, 14, 3, "Ch16 - Designing Service-Oriented Architecture" },
                    { 24, false, 15, 3, "Ch17 - Designing Component-Based Software Architecture" },
                    { 25, false, 16, 3, "Ch18 - Designing Concurrent and Real-Time Software Architecture" },
                    { 26, false, 17, 3, "Ch20 - Software Quality Attributes" },
                    { 27, false, 18, 3, "Ch21 - Client Server Software Architecture Case Study" },
                    { 28, false, 19, 3, "Ch22 - SOA Case Study - Online Shopping System" },
                    { 29, false, 20, 3, "Ch23 - Component-Based Software Architecture Case Study" },
                    { 30, false, 21, 3, "Ch24 - Real-Time Software Architecture Case Study" },
                    { 31, false, 22, 3, "SWD392 - Design Pattern" },
                    { 32, false, 23, 3, "SWD392 - GenAI" },
                    { 33, false, 24, 3, "SWD392 - RDS Document Case Study" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
