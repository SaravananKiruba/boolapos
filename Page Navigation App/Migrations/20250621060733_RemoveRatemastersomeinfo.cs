using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Page_Navigation_App.Migrations
{
    public partial class RemoveRatemastersomeinfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultGst",
                table: "RateMaster");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "RateMaster");

            migrationBuilder.DropColumn(
                name: "FinalRate",
                table: "RateMaster");

            migrationBuilder.DropColumn(
                name: "HallmarkingCharge",
                table: "RateMaster");

            migrationBuilder.DropColumn(
                name: "IsSpecialRate",
                table: "RateMaster");

            migrationBuilder.DropColumn(
                name: "MarketSource",
                table: "RateMaster");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "RateMaster");

            migrationBuilder.DropColumn(
                name: "PurchaseRate",
                table: "RateMaster");

            migrationBuilder.DropColumn(
                name: "SaleRate",
                table: "RateMaster");

            migrationBuilder.DropColumn(
                name: "WastagePercentage",
                table: "RateMaster");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultGst",
                table: "RateMaster",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "RateMaster",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalRate",
                table: "RateMaster",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HallmarkingCharge",
                table: "RateMaster",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsSpecialRate",
                table: "RateMaster",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MarketSource",
                table: "RateMaster",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "RateMaster",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PurchaseRate",
                table: "RateMaster",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SaleRate",
                table: "RateMaster",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WastagePercentage",
                table: "RateMaster",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
