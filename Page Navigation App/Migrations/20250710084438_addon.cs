using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Page_Navigation_App.Migrations
{
    public partial class addon : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderDetailID",
                table: "StockItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockItemID",
                table: "OrderDetails",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_OrderDetailID",
                table: "StockItems",
                column: "OrderDetailID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_StockItemID",
                table: "OrderDetails",
                column: "StockItemID");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_StockItems_StockItemID",
                table: "OrderDetails",
                column: "StockItemID",
                principalTable: "StockItems",
                principalColumn: "StockItemID");

            migrationBuilder.AddForeignKey(
                name: "FK_StockItems_OrderDetails_OrderDetailID",
                table: "StockItems",
                column: "OrderDetailID",
                principalTable: "OrderDetails",
                principalColumn: "OrderDetailID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_StockItems_StockItemID",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_StockItems_OrderDetails_OrderDetailID",
                table: "StockItems");

            migrationBuilder.DropIndex(
                name: "IX_StockItems_OrderDetailID",
                table: "StockItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderDetails_StockItemID",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "OrderDetailID",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "StockItemID",
                table: "OrderDetails");
        }
    }
}
