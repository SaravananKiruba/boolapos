using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Page_Navigation_App.Migrations
{
    public partial class JewelryShopEnhancements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalMetalWeight",
                table: "RepairJobs",
                type: "decimal(10,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AssignedTo",
                table: "RepairJobs",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHallmarkRequired",
                table: "RepairJobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MakingCharges",
                table: "RepairJobs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MetalRate",
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
                name: "Purity",
                table: "RepairJobs",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityChecks",
                table: "RepairJobs",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "RepairJobs",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoneDetails",
                table: "RepairJobs",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StoneWeight",
                table: "RepairJobs",
                type: "decimal(10,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "WorkmanRemarks",
                table: "RepairJobs",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

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
                name: "Collection",
                table: "Products",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Design",
                table: "Products",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HallmarkNumber",
                table: "Products",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCustomOrder",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "Products",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoneDetails",
                table: "Products",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StoneWeight",
                table: "Products",
                type: "decimal(10,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValueAdditionPercentage",
                table: "Products",
                type: "decimal(5,2)",
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
                name: "Size",
                table: "OrderDetails",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

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

            migrationBuilder.AddColumn<string>(
                name: "BangleSize",
                table: "Customers",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChainLength",
                table: "Customers",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FamilyDetails",
                table: "Customers",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGoldSchemeEnrolled",
                table: "Customers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPurchaseDate",
                table: "Customers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OutstandingAmount",
                table: "Customers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PreferredDesigns",
                table: "Customers",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredMetalType",
                table: "Customers",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RingSize",
                table: "Customers",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPurchases",
                table: "Customers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalMetalWeight",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "AssignedTo",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "IsHallmarkRequired",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "MakingCharges",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "MetalRate",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "NewHallmark",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "OldHallmark",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "Purity",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "QualityChecks",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "StoneDetails",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "StoneWeight",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "WorkmanRemarks",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "DefaultGst",
                table: "RateMaster");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
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
                name: "Collection",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Design",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HallmarkNumber",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsCustomOrder",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StoneDetails",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StoneWeight",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ValueAdditionPercentage",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HallmarkNumber",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "HallmarkingCharge",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "Size",
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
                name: "BangleSize",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ChainLength",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "FamilyDetails",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsGoldSchemeEnrolled",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LastPurchaseDate",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "OutstandingAmount",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PreferredDesigns",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PreferredMetalType",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "RingSize",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TotalPurchases",
                table: "Customers");
        }
    }
}
