using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Page_Navigation_App.Migrations
{
    public partial class prodcut : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateAdded",
                table: "Products",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateRemoved",
                table: "Products",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateAdded",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DateRemoved",
                table: "Products");
        }
    }
}
