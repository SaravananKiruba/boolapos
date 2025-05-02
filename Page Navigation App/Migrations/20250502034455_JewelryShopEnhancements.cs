using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Page_Navigation_App.Migrations
{
    public partial class JewelryShopEnhancements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserID = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EntityID = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    OldValues = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    NewValues = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Details = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IPAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "BusinessInfo",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BusinessName = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Website = table.Column<string>(type: "TEXT", nullable: true),
                    TaxId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessInfo", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    DefaultMakingCharges = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DefaultWastage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryID);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    GSTNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RegistrationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LoyaltyPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateOfAnniversary = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NotifyRateChanges = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CustomerType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    WhatsAppNumber = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    PreferredDesigns = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PreferredMetalType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RingSize = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    BangleSize = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ChainLength = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TotalPurchases = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsGoldSchemeEnrolled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastPurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FamilyDetails = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerID);
                });

            migrationBuilder.CreateTable(
                name: "EmailSettings",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SmtpServer = table.Column<string>(type: "TEXT", nullable: true),
                    SmtpPort = table.Column<int>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    Password = table.Column<string>(type: "TEXT", nullable: true),
                    SenderName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSettings", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "LogEntries",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UserID = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Component = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StackTrace = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ExceptionDetails = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Duration = table.Column<decimal>(type: "decimal(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEntries", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "NotificationLog",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Recipient = table.Column<string>(type: "TEXT", nullable: true),
                    Channel = table.Column<string>(type: "TEXT", nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    IsSuccessful = table.Column<bool>(type: "INTEGER", nullable: false),
                    Details = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLog", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "NotificationSettings",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EnableSMS = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableWhatsApp = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableEmailNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    SendBirthdayWishes = table.Column<bool>(type: "INTEGER", nullable: false),
                    SendAnniversaryWishes = table.Column<bool>(type: "INTEGER", nullable: false),
                    SendOrderConfirmations = table.Column<bool>(type: "INTEGER", nullable: false),
                    SendPaymentReminders = table.Column<bool>(type: "INTEGER", nullable: false),
                    SendRepairUpdates = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSettings", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "RateMaster",
                columns: table => new
                {
                    RateID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MetalType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Purity = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    EnteredBy = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SaleRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    PurchaseRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HallmarkingCharge = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DefaultGst = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MarketSource = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsSpecialRate = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateMaster", x => x.RateID);
                });

            migrationBuilder.CreateTable(
                name: "SecurityLogs",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserID = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsSuccessful = table.Column<bool>(type: "INTEGER", nullable: false),
                    IPAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Module = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AdditionalData = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityLogs", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    LowStockAlerts = table.Column<bool>(type: "INTEGER", nullable: false),
                    PaymentReminders = table.Column<bool>(type: "INTEGER", nullable: false),
                    LowStockThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    BackupPath = table.Column<string>(type: "TEXT", nullable: true),
                    AutoBackup = table.Column<bool>(type: "INTEGER", nullable: false),
                    BackupFrequencyDays = table.Column<int>(type: "INTEGER", nullable: false),
                    DarkMode = table.Column<bool>(type: "INTEGER", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: true),
                    Currency = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    SupplierID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SupplierName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ContactNumber = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    GSTNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ContactPerson = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    WhatsAppNumber = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    BankName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AccountNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IFSCCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PaymentTerms = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    LastPurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.SupplierID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "BLOB", nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "BLOB", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                });

            migrationBuilder.CreateTable(
                name: "Subcategories",
                columns: table => new
                {
                    SubcategoryID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SubcategoryName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CategoryID = table.Column<int>(type: "INTEGER", nullable: false),
                    SpecialMakingCharges = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    SpecialWastage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subcategories", x => x.SubcategoryID);
                    table.ForeignKey(
                        name: "FK_Subcategories_Categories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CustomerID = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PaymentType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DeliveryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    GSTNumber = table.Column<string>(type: "TEXT", nullable: true),
                    HasMetalExchange = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExchangeMetalWeight = table.Column<decimal>(type: "TEXT", nullable: false),
                    ExchangeMetalType = table.Column<string>(type: "TEXT", nullable: true),
                    ExchangeMetalPurity = table.Column<decimal>(type: "TEXT", nullable: false),
                    ExchangeValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    EMIMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    EMIAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderID);
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "Customers",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepairJobs",
                columns: table => new
                {
                    RepairJobID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    JobNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ItemDetails = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    MetalType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    WorkType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ReceiptDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PromisedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EstimatedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FinalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SMSNotificationSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    WhatsAppNotificationSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompletionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AdditionalMetalWeight = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    StoneWeight = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    StoneDetails = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    MetalRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MakingCharges = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Size = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    OldHallmark = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NewHallmark = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Purity = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    QualityChecks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsHallmarkRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    WorkmanRemarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AssignedTo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairJobs", x => x.RepairJobID);
                    table.ForeignKey(
                        name: "FK_RepairJobs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleName = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    UserID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleID);
                    table.ForeignKey(
                        name: "FK_Roles_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserRoleID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserID = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.UserRoleID);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FinalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WastagePercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MetalType = table.Column<string>(type: "TEXT", nullable: false),
                    Purity = table.Column<string>(type: "TEXT", nullable: false),
                    GrossWeight = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    NetWeight = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    MakingCharges = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StoneValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Barcode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsDeadStock = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Design = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Size = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StoneWeight = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    StoneDetails = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    HallmarkNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Collection = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ValueAdditionPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsCustomOrder = table.Column<bool>(type: "INTEGER", nullable: false),
                    CategoryID = table.Column<int>(type: "INTEGER", nullable: false),
                    SubcategoryID = table.Column<int>(type: "INTEGER", nullable: true),
                    SupplierID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductID);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Products_Subcategories_SubcategoryID",
                        column: x => x.SubcategoryID,
                        principalTable: "Subcategories",
                        principalColumn: "SubcategoryID");
                    table.ForeignKey(
                        name: "FK_Products_Suppliers_SupplierID",
                        column: x => x.SupplierID,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Finances",
                columns: table => new
                {
                    FinanceID = table.Column<string>(type: "TEXT", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TransactionType = table.Column<string>(type: "TEXT", nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaymentMode = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "TEXT", nullable: true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: true),
                    OrderReference = table.Column<int>(type: "INTEGER", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    RemainingAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    InterestRate = table.Column<decimal>(type: "TEXT", nullable: true),
                    NumberOfInstallments = table.Column<int>(type: "INTEGER", nullable: true),
                    InstallmentAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastPaymentDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextInstallmentDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    CGSTAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    SGSTAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    IGSTAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    GSTNumber = table.Column<string>(type: "TEXT", nullable: true),
                    MetalType = table.Column<string>(type: "TEXT", nullable: true),
                    MetalPurity = table.Column<string>(type: "TEXT", nullable: true),
                    MetalWeight = table.Column<decimal>(type: "TEXT", nullable: true),
                    MetalRate = table.Column<decimal>(type: "TEXT", nullable: true),
                    SupplierName = table.Column<string>(type: "TEXT", nullable: true),
                    WastagePercentage = table.Column<decimal>(type: "TEXT", nullable: true),
                    MakingCharges = table.Column<decimal>(type: "TEXT", nullable: true),
                    StoneValue = table.Column<decimal>(type: "TEXT", nullable: true),
                    HallmarkNumber = table.Column<string>(type: "TEXT", nullable: true),
                    ExchangeMetalWeight = table.Column<decimal>(type: "TEXT", nullable: true),
                    ExchangeMetalType = table.Column<string>(type: "TEXT", nullable: true),
                    ExchangeMetalPurity = table.Column<string>(type: "TEXT", nullable: true),
                    ExchangeValue = table.Column<decimal>(type: "TEXT", nullable: true),
                    SchemeTargetGrams = table.Column<decimal>(type: "TEXT", nullable: true),
                    SchemeCollectedGrams = table.Column<decimal>(type: "TEXT", nullable: true),
                    SchemeMonthlyAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    SchemeTenureMonths = table.Column<int>(type: "INTEGER", nullable: true),
                    SchemeMaturityDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SchemeStatus = table.Column<string>(type: "TEXT", nullable: true),
                    SchemeMaturityBonus = table.Column<decimal>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    OrderID = table.Column<int>(type: "INTEGER", nullable: true),
                    SupplierID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Finances", x => x.FinanceID);
                    table.ForeignKey(
                        name: "FK_Finances_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_Finances_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                    table.ForeignKey(
                        name: "FK_Finances_Suppliers_SupplierID",
                        column: x => x.SupplierID,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierID");
                });

            migrationBuilder.CreateTable(
                name: "OrderDetails",
                columns: table => new
                {
                    OrderDetailID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderID = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductID = table.Column<int>(type: "INTEGER", nullable: false),
                    GrossWeight = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    NetWeight = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    MetalRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaseAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MakingCharges = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StoneValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CGSTAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SGSTAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IGSTAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FinalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    WastagePercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    WastageAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HallmarkingCharge = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StoneWeight = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    StoneDetails = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    HallmarkNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ValueAdditionPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ValueAdditionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Size = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetails", x => x.OrderDetailID);
                    table.ForeignKey(
                        name: "FK_OrderDetails_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderDetails_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    StockID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductID = table.Column<int>(type: "INTEGER", nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSold = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AddedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsDeadStock = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.StockID);
                    table.ForeignKey(
                        name: "FK_Stocks_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Finances_CustomerId",
                table: "Finances",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Finances_OrderID",
                table: "Finances",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_Finances_SupplierID",
                table: "Finances",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_OrderID",
                table: "OrderDetails",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_ProductID",
                table: "OrderDetails",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerID",
                table: "Orders",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryID",
                table: "Products",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SubcategoryID",
                table: "Products",
                column: "SubcategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SupplierID",
                table: "Products",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobs_CustomerId",
                table: "RepairJobs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_UserID",
                table: "Roles",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProductID",
                table: "Stocks",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_Subcategories_CategoryID",
                table: "Subcategories",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserID",
                table: "UserRoles",
                column: "UserID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BusinessInfo");

            migrationBuilder.DropTable(
                name: "EmailSettings");

            migrationBuilder.DropTable(
                name: "Finances");

            migrationBuilder.DropTable(
                name: "LogEntries");

            migrationBuilder.DropTable(
                name: "NotificationLog");

            migrationBuilder.DropTable(
                name: "NotificationSettings");

            migrationBuilder.DropTable(
                name: "OrderDetails");

            migrationBuilder.DropTable(
                name: "RateMaster");

            migrationBuilder.DropTable(
                name: "RepairJobs");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "SecurityLogs");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Stocks");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Subcategories");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
