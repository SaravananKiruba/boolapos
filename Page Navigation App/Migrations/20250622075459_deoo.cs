using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Page_Navigation_App.Migrations
{
    public partial class deoo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BasePrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CGST",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EMIAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EMIMonths",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EWayBillNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExchangeMetalPurity",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExchangeMetalType",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExchangeMetalWeight",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExchangeValue",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "GSTNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "HSNCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "HallmarkingCharges",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "HasMetalExchange",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IGST",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsDelivered",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsGSTRegisteredCustomer",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TagReferences",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TaxApplicable",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BaseAmount",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "CGSTAmount",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "FinalAmount",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "GrossWeight",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "HSNCode",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "IGSTAmount",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "MakingCharges",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "MetalRate",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "NetWeight",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "SGSTAmount",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "StoneValue",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "TaxableAmount",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "TotalPrice",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "WastagePercentage",
                table: "OrderDetails");

            migrationBuilder.RenameColumn(
                name: "FinalPrice",
                table: "Products",
                newName: "ProductPrice");

            migrationBuilder.RenameColumn(
                name: "SGST",
                table: "Orders",
                newName: "PriceBeforeTax");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProductPrice",
                table: "Products",
                newName: "FinalPrice");

            migrationBuilder.RenameColumn(
                name: "PriceBeforeTax",
                table: "Orders",
                newName: "SGST");

            migrationBuilder.AddColumn<decimal>(
                name: "BasePrice",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CGST",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress",
                table: "Orders",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryDate",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EMIAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "EMIMonths",
                table: "Orders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EWayBillNumber",
                table: "Orders",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExchangeMetalPurity",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExchangeMetalType",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeMetalWeight",
                table: "Orders",
                type: "decimal(10,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeValue",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "GSTNumber",
                table: "Orders",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HSNCode",
                table: "Orders",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HallmarkingCharges",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "HasMetalExchange",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "IGST",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsDelivered",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsGSTRegisteredCustomer",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TagReferences",
                table: "Orders",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TaxApplicable",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseAmount",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CGSTAmount",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalAmount",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossWeight",
                table: "OrderDetails",
                type: "decimal(10,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "HSNCode",
                table: "OrderDetails",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IGSTAmount",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MakingCharges",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MetalRate",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetWeight",
                table: "OrderDetails",
                type: "decimal(10,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SGSTAmount",
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

            migrationBuilder.AddColumn<decimal>(
                name: "StoneValue",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxableAmount",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPrice",
                table: "OrderDetails",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WastagePercentage",
                table: "OrderDetails",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
