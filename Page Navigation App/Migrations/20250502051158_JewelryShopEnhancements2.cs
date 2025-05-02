using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Page_Navigation_App.Migrations
{
    public partial class JewelryShopEnhancements2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "WhatsAppNumber",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "EstimatedAmount",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "JobNumber",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "MakingCharges",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "NewHallmark",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "OldHallmark",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "StoneWeight",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "IsDeadStock",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "OrderStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "HallmarkNumber",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "HallmarkingCharge",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "StoneDetails",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "StoneWeight",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ValueAdditionAmount",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ValueAdditionPercentage",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "WastageAmount",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "WastagePercentage",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "ExchangeMetalPurity",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "ExchangeMetalType",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "ExchangeMetalWeight",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "ExchangeValue",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "GSTNumber",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "HallmarkNumber",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "MakingCharges",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "SchemeCollectedGrams",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "SchemeMaturityBonus",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "SchemeMaturityDate",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "SchemeMonthlyAmount",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "SchemeStatus",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "SchemeTargetGrams",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "StoneValue",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Finances");

            migrationBuilder.DropColumn(
                name: "WastagePercentage",
                table: "Finances");

            migrationBuilder.RenameColumn(
                name: "LastPurchaseDate",
                table: "Suppliers",
                newName: "PreferredPaymentTerms");

            migrationBuilder.RenameColumn(
                name: "CurrentBalance",
                table: "Suppliers",
                newName: "OutstandingAmount");

            migrationBuilder.RenameColumn(
                name: "ContactNumber",
                table: "Suppliers",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "WorkmanRemarks",
                table: "RepairJobs",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "Weight",
                table: "RepairJobs",
                newName: "ItemWeight");

            migrationBuilder.RenameColumn(
                name: "QualityChecks",
                table: "RepairJobs",
                newName: "CustomerComments");

            migrationBuilder.RenameColumn(
                name: "PromisedDate",
                table: "RepairJobs",
                newName: "ExpectedDeliveryDate");

            migrationBuilder.RenameColumn(
                name: "MetalRate",
                table: "RepairJobs",
                newName: "EstimatedCost");

            migrationBuilder.RenameColumn(
                name: "ItemDetails",
                table: "RepairJobs",
                newName: "WorkDescription");

            migrationBuilder.RenameColumn(
                name: "IsHallmarkRequired",
                table: "RepairJobs",
                newName: "IsStoneProvided");

            migrationBuilder.RenameColumn(
                name: "RepairJobID",
                table: "RepairJobs",
                newName: "RepairID");

            migrationBuilder.RenameColumn(
                name: "SchemeTenureMonths",
                table: "Finances",
                newName: "InstallmentNumber");

            migrationBuilder.AlterColumn<string>(
                name: "MetalType",
                table: "RepairJobs",
                type: "TEXT",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "AdditionalMetalWeight",
                table: "RepairJobs",
                type: "decimal(10,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,3)");

            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalMetalCost",
                table: "RepairJobs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AdvanceAmount",
                table: "RepairJobs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryDate",
                table: "RepairJobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemDescription",
                table: "RepairJobs",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "RepairJobs",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "StoneCharge",
                table: "RepairJobs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "RateMaster",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PurchaseRate",
                table: "RateMaster",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "MakingCharges",
                table: "Products",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "Products",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "Orders",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeValue",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeMetalWeight",
                table: "Orders",
                type: "decimal(10,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "ExchangeMetalPurity",
                table: "Orders",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "EMIMonths",
                table: "Orders",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<decimal>(
                name: "EMIAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress",
                table: "Orders",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDelivered",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "TransactionType",
                table: "Finances",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Finances",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "SGSTAmount",
                table: "Finances",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RemainingAmount",
                table: "Finances",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMode",
                table: "Finances",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MetalWeight",
                table: "Finances",
                type: "decimal(10,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MetalRate",
                table: "Finances",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "InterestRate",
                table: "Finances",
                type: "decimal(5,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "InstallmentAmount",
                table: "Finances",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "IGSTAmount",
                table: "Finances",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Finances",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CGSTAmount",
                table: "Finances",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Finances",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.CreateTable(
                name: "EMIs",
                columns: table => new
                {
                    EMIID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderID = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InstallmentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InterestRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TotalInstallments = table.Column<int>(type: "INTEGER", nullable: false),
                    RemainingInstallments = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PaymentDay = table.Column<int>(type: "INTEGER", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastPaymentDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextPaymentDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMIs", x => x.EMIID);
                    table.ForeignKey(
                        name: "FK_EMIs_Customers_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Customers",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_EMIs_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EMIs_OrderID",
                table: "EMIs",
                column: "OrderID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EMIs");

            migrationBuilder.DropColumn(
                name: "AdditionalMetalCost",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "AdvanceAmount",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "DeliveryDate",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "ItemDescription",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "StoneCharge",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsDelivered",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "PreferredPaymentTerms",
                table: "Suppliers",
                newName: "LastPurchaseDate");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "Suppliers",
                newName: "ContactNumber");

            migrationBuilder.RenameColumn(
                name: "OutstandingAmount",
                table: "Suppliers",
                newName: "CurrentBalance");

            migrationBuilder.RenameColumn(
                name: "WorkDescription",
                table: "RepairJobs",
                newName: "ItemDetails");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "RepairJobs",
                newName: "WorkmanRemarks");

            migrationBuilder.RenameColumn(
                name: "ItemWeight",
                table: "RepairJobs",
                newName: "Weight");

            migrationBuilder.RenameColumn(
                name: "IsStoneProvided",
                table: "RepairJobs",
                newName: "IsHallmarkRequired");

            migrationBuilder.RenameColumn(
                name: "ExpectedDeliveryDate",
                table: "RepairJobs",
                newName: "PromisedDate");

            migrationBuilder.RenameColumn(
                name: "EstimatedCost",
                table: "RepairJobs",
                newName: "MetalRate");

            migrationBuilder.RenameColumn(
                name: "CustomerComments",
                table: "RepairJobs",
                newName: "QualityChecks");

            migrationBuilder.RenameColumn(
                name: "RepairID",
                table: "RepairJobs",
                newName: "RepairJobID");

            migrationBuilder.RenameColumn(
                name: "InstallmentNumber",
                table: "Finances",
                newName: "SchemeTenureMonths");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Suppliers",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTerms",
                table: "Suppliers",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppNumber",
                table: "Suppliers",
                type: "TEXT",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MetalType",
                table: "RepairJobs",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AdditionalMetalWeight",
                table: "RepairJobs",
                type: "decimal(10,3)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,3)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "RepairJobs",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedAmount",
                table: "RepairJobs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "JobNumber",
                table: "RepairJobs",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MakingCharges",
                table: "RepairJobs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NewHallmark",
                table: "RepairJobs",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OldHallmark",
                table: "RepairJobs",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "RepairJobs",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StoneWeight",
                table: "RepairJobs",
                type: "decimal(10,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "RateMaster",
                type: "TEXT",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "PurchaseRate",
                table: "RateMaster",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MakingCharges",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "Products",
                type: "TEXT",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeadStock",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Products",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "Orders",
                type: "TEXT",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeValue",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeMetalWeight",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeMetalPurity",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EMIMonths",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "EMIAmount",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "OrderStatus",
                table: "Orders",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "HallmarkNumber",
                table: "OrderDetails",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HallmarkingCharge",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "StoneDetails",
                table: "OrderDetails",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StoneWeight",
                table: "OrderDetails",
                type: "decimal(10,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValueAdditionAmount",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValueAdditionPercentage",
                table: "OrderDetails",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WastageAmount",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WastagePercentage",
                table: "OrderDetails",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "TransactionType",
                table: "Finances",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Finances",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "SGSTAmount",
                table: "Finances",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RemainingAmount",
                table: "Finances",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMode",
                table: "Finances",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "MetalWeight",
                table: "Finances",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MetalRate",
                table: "Finances",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "InterestRate",
                table: "Finances",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "InstallmentAmount",
                table: "Finances",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "IGSTAmount",
                table: "Finances",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Finances",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "CGSTAmount",
                table: "Finances",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Finances",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Finances",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Finances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExchangeMetalPurity",
                table: "Finances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExchangeMetalType",
                table: "Finances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeMetalWeight",
                table: "Finances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeValue",
                table: "Finances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GSTNumber",
                table: "Finances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HallmarkNumber",
                table: "Finances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MakingCharges",
                table: "Finances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SchemeCollectedGrams",
                table: "Finances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SchemeMaturityBonus",
                table: "Finances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SchemeMaturityDate",
                table: "Finances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SchemeMonthlyAmount",
                table: "Finances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchemeStatus",
                table: "Finances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SchemeTargetGrams",
                table: "Finances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StoneValue",
                table: "Finances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Finances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Finances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WastagePercentage",
                table: "Finances",
                type: "TEXT",
                nullable: true);
        }
    }
}
