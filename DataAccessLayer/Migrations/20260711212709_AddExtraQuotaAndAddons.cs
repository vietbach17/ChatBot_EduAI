using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddExtraQuotaAndAddons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExtraQuestionQuota",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "UseExtraQuota",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "PlanId",
                table: "PaymentTransactions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "AddonId",
                table: "PaymentTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AddonPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    QuotaAmount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddonPackages", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AddonPackages",
                columns: new[] { "Id", "IsActive", "Name", "Price", "QuotaAmount" },
                values: new object[,]
                {
                    { 1, true, "Gói Mini (Cấp tốc)", 10000m, 15 },
                    { 2, true, "Gói Standard (Cứu cánh)", 20000m, 40 },
                    { 3, true, "Gói Premium (Chạy nước rút)", 50000m, 120 }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ExtraQuestionQuota", "UseExtraQuota" },
                values: new object[] { 0, false });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ExtraQuestionQuota", "UseExtraQuota" },
                values: new object[] { 0, false });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ExtraQuestionQuota", "UseExtraQuota" },
                values: new object[] { 0, false });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_AddonId",
                table: "PaymentTransactions",
                column: "AddonId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransactions_AddonPackages_AddonId",
                table: "PaymentTransactions",
                column: "AddonId",
                principalTable: "AddonPackages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_AddonPackages_AddonId",
                table: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "AddonPackages");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_AddonId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "ExtraQuestionQuota",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UseExtraQuota",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AddonId",
                table: "PaymentTransactions");

            migrationBuilder.AlterColumn<int>(
                name: "PlanId",
                table: "PaymentTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
