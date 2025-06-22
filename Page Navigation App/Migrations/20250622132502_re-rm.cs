using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Page_Navigation_App.Migrations
{
    public partial class rerm : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepairJobs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RepairJobs",
                columns: table => new
                {
                    RepairID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdditionalMetalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AdditionalMetalWeight = table.Column<decimal>(type: "decimal(10,3)", nullable: true),
                    AdvanceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AssignedTo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CompletionDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CustomerComments = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DeliveryDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedDeliveryDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    IsStoneProvided = table.Column<bool>(type: "INTEGER", nullable: false),
                    ItemDescription = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ItemPhotoUrl = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    ItemWeight = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    MetalType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Purity = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    ReceiptDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StoneCharge = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StoneDetails = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    WorkDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    WorkStartDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    WorkType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairJobs", x => x.RepairID);
                    table.ForeignKey(
                        name: "FK_RepairJobs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobs_CustomerId",
                table: "RepairJobs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobs_Status",
                table: "RepairJobs",
                column: "Status");
        }
    }
}
