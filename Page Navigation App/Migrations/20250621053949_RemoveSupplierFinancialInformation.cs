using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Page_Navigation_App.Migrations
{
    public partial class RemoveSupplierFinancialInformation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Finances_Suppliers_SupplierID",
                table: "Finances");

            migrationBuilder.DropIndex(
                name: "IX_Finances_SupplierID",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CreditLimit",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "IFSCCode",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "OutstandingAmount",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "PreferredPaymentTerms",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "SupplierID",
                table: "Finances");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "Suppliers",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Suppliers",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditLimit",
                table: "Suppliers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "IFSCCode",
                table: "Suppliers",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OutstandingAmount",
                table: "Suppliers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PreferredPaymentTerms",
                table: "Suppliers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplierID",
                table: "Finances",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Finances_SupplierID",
                table: "Finances",
                column: "SupplierID");

            migrationBuilder.AddForeignKey(
                name: "FK_Finances_Suppliers_SupplierID",
                table: "Finances",
                column: "SupplierID",
                principalTable: "Suppliers",
                principalColumn: "SupplierID");
        }
    }
}
