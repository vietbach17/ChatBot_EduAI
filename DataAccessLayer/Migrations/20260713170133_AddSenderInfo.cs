using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddSenderInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActualTransferContent",
                table: "PaymentTransactions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderAccountInfo",
                table: "PaymentTransactions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AddonPackages",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Gói Ultra (Chạy nước rút)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualTransferContent",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "SenderAccountInfo",
                table: "PaymentTransactions");

            migrationBuilder.UpdateData(
                table: "AddonPackages",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Gói Premium (Chạy nước rút)");
        }
    }
}
