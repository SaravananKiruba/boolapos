using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Page_Navigation_App.Migrations
{
    public partial class der : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GstAmount",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "GstPercentage",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsGstApplicable",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ValueAdditionPercentage",
                table: "Products");

            migrationBuilder.AlterColumn<decimal>(
                name: "MakingCharges",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AddColumn<string>(
                name: "AHCCode",
                table: "Products",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TaxApplicable",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AHCCode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TaxApplicable",
                table: "Orders");

            migrationBuilder.AlterColumn<decimal>(
                name: "MakingCharges",
                table: "Products",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<decimal>(
                name: "GstAmount",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GstPercentage",
                table: "Products",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsGstApplicable",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ValueAdditionPercentage",
                table: "Products",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
