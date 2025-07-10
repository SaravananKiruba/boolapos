using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Page_Navigation_App.Migrations
{
    /// <inheritdoc />
    public partial class AddStockItemOrderDetailRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add StockItemID to OrderDetails table
            migrationBuilder.AddColumn<int>(
                name: "StockItemID",
                table: "OrderDetails",
                type: "INTEGER",
                nullable: true);

            // Add OrderDetailID to StockItems table
            migrationBuilder.AddColumn<int>(
                name: "OrderDetailID",
                table: "StockItems",
                type: "INTEGER",
                nullable: true);

            // Create foreign key relationships
            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_StockItemID",
                table: "OrderDetails",
                column: "StockItemID");

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_OrderDetailID",
                table: "StockItems",
                column: "OrderDetailID");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove foreign keys
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_StockItems_StockItemID",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_StockItems_OrderDetails_OrderDetailID",
                table: "StockItems");

            // Remove indexes
            migrationBuilder.DropIndex(
                name: "IX_OrderDetails_StockItemID",
                table: "OrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_StockItems_OrderDetailID",
                table: "StockItems");

            // Remove columns
            migrationBuilder.DropColumn(
                name: "StockItemID",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "OrderDetailID",
                table: "StockItems");
        }
    }
}
