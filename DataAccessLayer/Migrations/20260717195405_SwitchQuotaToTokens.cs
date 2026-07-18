using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class SwitchQuotaToTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Đổi đơn vị quota từ "lượt hỏi" sang "token" nhưng GIỮ dữ liệu:
            // rename cột + nâng kiểu int -> bigint thay vì drop/add.
            migrationBuilder.RenameColumn(
                name: "ExtraQuestionQuota",
                table: "Users",
                newName: "ExtraTokenQuota");

            migrationBuilder.RenameColumn(
                name: "MonthlyQuestionCount",
                table: "Users",
                newName: "MonthlyTokensUsed");

            migrationBuilder.RenameColumn(
                name: "ShortTermQuestionCount",
                table: "Users",
                newName: "ShortTermTokensUsed");

            migrationBuilder.AlterColumn<long>(
                name: "ExtraTokenQuota",
                table: "Users",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "MonthlyTokensUsed",
                table: "Users",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "ShortTermTokensUsed",
                table: "Users",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            // Quy đổi số dư lượt hỏi dự phòng đã mua sang token (~5.000 token / lượt)
            // để người dùng đã trả tiền không bị mất quyền lợi.
            migrationBuilder.Sql(@"UPDATE ""Users"" SET ""ExtraTokenQuota"" = ""ExtraTokenQuota"" * 5000;");

            // Số lượt ĐÃ DÙNG trong chu kỳ hiện tại không quy đổi được sang token — reset về 0
            // (người dùng được lợi nhẹ một chu kỳ, chấp nhận được).
            migrationBuilder.Sql(@"UPDATE ""Users"" SET ""MonthlyTokensUsed"" = 0, ""ShortTermTokensUsed"" = 0;");

            // Các gói addon do admin tự tạo thêm (ngoài 3 gói seed bên dưới) cũng quy đổi sang token.
            migrationBuilder.Sql(@"UPDATE ""AddonPackages"" SET ""QuotaAmount"" = ""QuotaAmount"" * 5000 WHERE ""Id"" > 3;");

            migrationBuilder.CreateTable(
                name: "TokenUsageLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Feature = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Model = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PromptTokens = table.Column<int>(type: "integer", nullable: false),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    IsEstimated = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenUsageLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AddonPackages",
                keyColumn: "Id",
                keyValue: 1,
                column: "QuotaAmount",
                value: 75000);

            migrationBuilder.UpdateData(
                table: "AddonPackages",
                keyColumn: "Id",
                keyValue: 2,
                column: "QuotaAmount",
                value: 200000);

            migrationBuilder.UpdateData(
                table: "AddonPackages",
                keyColumn: "Id",
                keyValue: 3,
                column: "QuotaAmount",
                value: 600000);

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 1,
                column: "Features",
                value: "[\"Hỏi đáp AI cơ bản\", \"Độ trễ phản hồi bình thường\", \"Giới hạn 50.000 token / 5 giờ\"]");

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 2,
                column: "Features",
                value: "[\"Ưu tiên xử lý câu hỏi\", \"Tốc độ phản hồi AI nhanh hơn\", \"Giới hạn 100.000 token / 5 giờ\", \"Hỗ trợ tài liệu đính kèm\"]");

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 3,
                column: "Features",
                value: "[\"Không giới hạn token\", \"AI phản hồi tức thì\", \"Mô hình AI cao cấp nhất\", \"Hỗ trợ ưu tiên 24/7\"]");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ExtraTokenQuota", "MonthlyTokensUsed", "ShortTermTokensUsed" },
                values: new object[] { 0L, 0L, 0L });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ExtraTokenQuota", "MonthlyTokensUsed", "ShortTermTokensUsed" },
                values: new object[] { 0L, 0L, 0L });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ExtraTokenQuota", "MonthlyTokensUsed", "ShortTermTokensUsed" },
                values: new object[] { 0L, 0L, 0L });

            migrationBuilder.CreateIndex(
                name: "IX_TokenUsageLogs_UserId_CreatedAt",
                table: "TokenUsageLogs",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenUsageLogs");

            // Quy đổi ngược token -> lượt hỏi (5.000 token = 1 lượt) trước khi thu hẹp kiểu cột.
            migrationBuilder.Sql(@"UPDATE ""Users"" SET ""ExtraTokenQuota"" = ""ExtraTokenQuota"" / 5000, ""MonthlyTokensUsed"" = 0, ""ShortTermTokensUsed"" = 0;");
            migrationBuilder.Sql(@"UPDATE ""AddonPackages"" SET ""QuotaAmount"" = ""QuotaAmount"" / 5000 WHERE ""Id"" > 3;");

            migrationBuilder.AlterColumn<int>(
                name: "ExtraTokenQuota",
                table: "Users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "MonthlyTokensUsed",
                table: "Users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "ShortTermTokensUsed",
                table: "Users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.RenameColumn(
                name: "ExtraTokenQuota",
                table: "Users",
                newName: "ExtraQuestionQuota");

            migrationBuilder.RenameColumn(
                name: "MonthlyTokensUsed",
                table: "Users",
                newName: "MonthlyQuestionCount");

            migrationBuilder.RenameColumn(
                name: "ShortTermTokensUsed",
                table: "Users",
                newName: "ShortTermQuestionCount");

            migrationBuilder.UpdateData(
                table: "AddonPackages",
                keyColumn: "Id",
                keyValue: 1,
                column: "QuotaAmount",
                value: 15);

            migrationBuilder.UpdateData(
                table: "AddonPackages",
                keyColumn: "Id",
                keyValue: 2,
                column: "QuotaAmount",
                value: 40);

            migrationBuilder.UpdateData(
                table: "AddonPackages",
                keyColumn: "Id",
                keyValue: 3,
                column: "QuotaAmount",
                value: 120);

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 1,
                column: "Features",
                value: "[\"Hỏi đáp AI cơ bản\", \"Độ trễ phản hồi bình thường\", \"Giới hạn 5 câu hỏi / 5 giờ\"]");

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 2,
                column: "Features",
                value: "[\"Ưu tiên xử lý câu hỏi\", \"Tốc độ phản hồi AI nhanh hơn\", \"Giới hạn 20 câu hỏi / 5 giờ\", \"Hỗ trợ tài liệu đính kèm\"]");

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 3,
                column: "Features",
                value: "[\"Không giới hạn số câu hỏi\", \"AI phản hồi tức thì\", \"Mô hình AI cao cấp nhất\", \"Hỗ trợ ưu tiên 24/7\"]");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ExtraQuestionQuota", "MonthlyQuestionCount", "ShortTermQuestionCount" },
                values: new object[] { 0, 0, 0 });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ExtraQuestionQuota", "MonthlyQuestionCount", "ShortTermQuestionCount" },
                values: new object[] { 0, 0, 0 });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ExtraQuestionQuota", "MonthlyQuestionCount", "ShortTermQuestionCount" },
                values: new object[] { 0, 0, 0 });
        }
    }
}
