using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExchangeRateProject.Migrations
{
    /// <inheritdoc />
    public partial class AddRateIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CurrencyCode",
                table: "Rates",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "BankName",
                table: "Rates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Rates_BankName_CurrencyCode_FetchDate",
                table: "Rates",
                columns: new[] { "BankName", "CurrencyCode", "FetchDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Rates_FetchDate",
                table: "Rates",
                column: "FetchDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rates_BankName_CurrencyCode_FetchDate",
                table: "Rates");

            migrationBuilder.DropIndex(
                name: "IX_Rates_FetchDate",
                table: "Rates");

            migrationBuilder.AlterColumn<string>(
                name: "CurrencyCode",
                table: "Rates",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "BankName",
                table: "Rates",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
