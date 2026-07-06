using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    MonthlyQuestionLimit = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    SubscriptionPlan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubscriptionExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MonthlyQuestionCount = table.Column<int>(type: "integer", nullable: false),
                    QuotaResetDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "Id", "Description", "IsActive", "MonthlyQuestionLimit", "Name", "Price", "SortOrder" },
                values: new object[,]
                {
                    { 1, "GÃ³i miá»…n phÃ­ cÆ¡ báº£n", true, 5, "Free", 0m, 1 },
                    { 2, "GÃ³i cÆ¡ báº£n 100 cÃ¢u há»i/thÃ¡ng", true, 100, "Basic", 50000m, 2 },
                    { 3, "GÃ³i cao cáº¥p khÃ´ng giá»›i háº¡n", true, -1, "Premium", 150000m, 3 }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "IsDeleted", "MonthlyQuestionCount", "PasswordHash", "QuotaResetDate", "Role", "SubscriptionExpiry", "SubscriptionPlan", "Username" },
                values: new object[,]
                {
                    { 1, null, false, 0, "student123", null, "Student", null, "Free", "student" },
                    { 2, null, false, 0, "lecturer123", null, "Lecturer", null, "Free", "lecturer" },
                    { 3, null, false, 0, "admin123", null, "Admin", null, "Free", "admin" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
