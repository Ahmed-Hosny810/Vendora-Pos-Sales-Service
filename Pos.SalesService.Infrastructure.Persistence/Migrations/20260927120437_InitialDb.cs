using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.SalesService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sales");

            migrationBuilder.CreateTable(
                name: "CashierShifts",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TerminalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpeningCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ExpectedCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Difference = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashierShifts", x => x.Id);
                    table.UniqueConstraint("AK_CashierShifts_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_CashierShifts_Cash", "[OpeningCash] >= 0 AND ([ClosingCash] IS NULL OR [ClosingCash] >= 0)");
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Area = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.UniqueConstraint("AK_Customers_TenantId_Id", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsCash = table.Column<bool>(type: "bit", nullable: false),
                    RequiresReferenceNumber = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                    table.UniqueConstraint("AK_PaymentMethods_TenantId_Id", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "ReceiptSequences",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastNumber = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptSequences", x => x.Id);
                    table.CheckConstraint("CK_ReceiptSequences_LastNumber", "[LastNumber] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Sales",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TerminalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StockReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", nullable: false),
                    PricesIncludeTax = table.Column<bool>(type: "bit", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ChangeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CustomerNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomerPhoneSnapshot = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DeliveryAddressSnapshot = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales", x => x.Id);
                    table.UniqueConstraint("AK_Sales_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Sales_Amounts", "[Subtotal] >= 0 AND [DiscountTotal] >= 0 AND [TaxTotal] >= 0 AND [Total] >= 0 AND [PaidAmount] >= 0 AND [ChangeAmount] >= 0 AND [ChangeAmount] <= [PaidAmount]");
                    table.ForeignKey(
                        name: "FK_Sales_CashierShifts_TenantId_ShiftId",
                        columns: x => new { x.TenantId, x.ShiftId },
                        principalSchema: "sales",
                        principalTable: "CashierShifts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sales_Customers_TenantId_CustomerId",
                        columns: x => new { x.TenantId, x.CustomerId },
                        principalSchema: "sales",
                        principalTable: "Customers",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Returns",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalSaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReturnNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProcessedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Returns", x => x.Id);
                    table.UniqueConstraint("AK_Returns_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.UniqueConstraint("AK_Returns_TenantId_Id_OriginalSaleId", x => new { x.TenantId, x.Id, x.OriginalSaleId });
                    table.CheckConstraint("CK_Returns_RefundAmount", "[RefundAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_Returns_Sales_TenantId_OriginalSaleId",
                        columns: x => new { x.TenantId, x.OriginalSaleId },
                        principalSchema: "sales",
                        principalTable: "Sales",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleItems",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemNumber = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VariantNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SkuSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BarcodeSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UnitNameSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TrackInventorySnapshot = table.Column<bool>(type: "bit", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleItems", x => x.Id);
                    table.UniqueConstraint("AK_SaleItems_TenantId_SaleId_Id", x => new { x.TenantId, x.SaleId, x.Id });
                    table.CheckConstraint("CK_SaleItems_Amounts", "[UnitPrice] >= 0 AND [UnitCost] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [LineTotal] >= 0 AND [TaxRate] >= 0 AND [TaxRate] <= 100");
                    table.CheckConstraint("CK_SaleItems_ItemNumber", "[ItemNumber] > 0");
                    table.CheckConstraint("CK_SaleItems_Quantities", "[Quantity] > 0 AND [ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [Quantity]");
                    table.ForeignKey(
                        name: "FK_SaleItems_Sales_TenantId_SaleId",
                        columns: x => new { x.TenantId, x.SaleId },
                        principalSchema: "sales",
                        principalTable: "Sales",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalePayments",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentMethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentMethodNameSnapshot = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PaymentMethodCodeSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCashSnapshot = table.Column<bool>(type: "bit", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ChangeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReceivedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalePayments", x => x.Id);
                    table.CheckConstraint("CK_SalePayments_Amounts", "[Amount] > 0 AND [ChangeAmount] >= 0 AND [ChangeAmount] <= [Amount] AND ([IsCashSnapshot] = 1 OR [ChangeAmount] = 0)");
                    table.ForeignKey(
                        name: "FK_SalePayments_PaymentMethods_TenantId_PaymentMethodId",
                        columns: x => new { x.TenantId, x.PaymentMethodId },
                        principalSchema: "sales",
                        principalTable: "PaymentMethods",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalePayments_Sales_TenantId_SaleId",
                        columns: x => new { x.TenantId, x.SaleId },
                        principalSchema: "sales",
                        principalTable: "Sales",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleStatusHistory",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleStatusHistory_Sales_TenantId_SaleId",
                        columns: x => new { x.TenantId, x.SaleId },
                        principalSchema: "sales",
                        principalTable: "Sales",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefundPayments",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentMethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentMethodNameSnapshot = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PaymentMethodCodeSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCashSnapshot = table.Column<bool>(type: "bit", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaidByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundPayments", x => x.Id);
                    table.CheckConstraint("CK_RefundPayments_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_RefundPayments_CashierShifts_TenantId_ShiftId",
                        columns: x => new { x.TenantId, x.ShiftId },
                        principalSchema: "sales",
                        principalTable: "CashierShifts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefundPayments_PaymentMethods_TenantId_PaymentMethodId",
                        columns: x => new { x.TenantId, x.PaymentMethodId },
                        principalSchema: "sales",
                        principalTable: "PaymentMethods",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefundPayments_Returns_TenantId_ReturnId",
                        columns: x => new { x.TenantId, x.ReturnId },
                        principalSchema: "sales",
                        principalTable: "Returns",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReturnItems",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalSaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalSaleItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Restock = table.Column<bool>(type: "bit", nullable: false),
                    StockCondition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnItems", x => x.Id);
                    table.CheckConstraint("CK_ReturnItems_Quantity", "[Quantity] > 0 AND [RefundAmount] >= 0");
                    table.CheckConstraint("CK_ReturnItems_Restock", "[Restock] = 0 OR [StockCondition] = N'Sellable'");
                    table.ForeignKey(
                        name: "FK_ReturnItems_Returns_TenantId_ReturnId_OriginalSaleId",
                        columns: x => new { x.TenantId, x.ReturnId, x.OriginalSaleId },
                        principalSchema: "sales",
                        principalTable: "Returns",
                        principalColumns: new[] { "TenantId", "Id", "OriginalSaleId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnItems_SaleItems_TenantId_OriginalSaleId_OriginalSaleItemId",
                        columns: x => new { x.TenantId, x.OriginalSaleId, x.OriginalSaleItemId },
                        principalSchema: "sales",
                        principalTable: "SaleItems",
                        principalColumns: new[] { "TenantId", "SaleId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleDiscounts",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DiscountType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleDiscounts", x => x.Id);
                    table.CheckConstraint("CK_SaleDiscounts_Value", "[Value] > 0 AND [Amount] >= 0 AND ([DiscountType] <> N'Percentage' OR [Value] <= 100)");
                    table.ForeignKey(
                        name: "FK_SaleDiscounts_SaleItems_TenantId_SaleId_SaleItemId",
                        columns: x => new { x.TenantId, x.SaleId, x.SaleItemId },
                        principalSchema: "sales",
                        principalTable: "SaleItems",
                        principalColumns: new[] { "TenantId", "SaleId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleDiscounts_Sales_TenantId_SaleId",
                        columns: x => new { x.TenantId, x.SaleId },
                        principalSchema: "sales",
                        principalTable: "Sales",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashierShifts_TenantId_BranchId_OpenedAt",
                schema: "sales",
                table: "CashierShifts",
                columns: new[] { "TenantId", "BranchId", "OpenedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CashierShifts_TenantId_CashierUserId",
                schema: "sales",
                table: "CashierShifts",
                columns: new[] { "TenantId", "CashierUserId" },
                unique: true,
                filter: "[Status] = N'Open'");

            migrationBuilder.CreateIndex(
                name: "IX_CashierShifts_TenantId_TerminalId",
                schema: "sales",
                table: "CashierShifts",
                columns: new[] { "TenantId", "TerminalId" },
                unique: true,
                filter: "[Status] = N'Open'");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_Phone",
                schema: "sales",
                table: "Customers",
                columns: new[] { "TenantId", "Phone" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_TenantId_Code",
                schema: "sales",
                table: "PaymentMethods",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptSequences_TenantId_BranchId_DocumentType",
                schema: "sales",
                table: "ReceiptSequences",
                columns: new[] { "TenantId", "BranchId", "DocumentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptSequences_TenantId_Prefix",
                schema: "sales",
                table: "ReceiptSequences",
                columns: new[] { "TenantId", "Prefix" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefundPayments_TenantId_IdempotencyKey",
                schema: "sales",
                table: "RefundPayments",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RefundPayments_TenantId_PaymentMethodId",
                schema: "sales",
                table: "RefundPayments",
                columns: new[] { "TenantId", "PaymentMethodId" });

            migrationBuilder.CreateIndex(
                name: "IX_RefundPayments_TenantId_ReturnId_Status",
                schema: "sales",
                table: "RefundPayments",
                columns: new[] { "TenantId", "ReturnId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RefundPayments_TenantId_ShiftId",
                schema: "sales",
                table: "RefundPayments",
                columns: new[] { "TenantId", "ShiftId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnItems_TenantId_OriginalSaleId_OriginalSaleItemId",
                schema: "sales",
                table: "ReturnItems",
                columns: new[] { "TenantId", "OriginalSaleId", "OriginalSaleItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnItems_TenantId_ReturnId_OriginalSaleId",
                schema: "sales",
                table: "ReturnItems",
                columns: new[] { "TenantId", "ReturnId", "OriginalSaleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId_BranchId_CreatedAt",
                schema: "sales",
                table: "Returns",
                columns: new[] { "TenantId", "BranchId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId_IdempotencyKey",
                schema: "sales",
                table: "Returns",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId_OriginalSaleId",
                schema: "sales",
                table: "Returns",
                columns: new[] { "TenantId", "OriginalSaleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId_ReturnNumber",
                schema: "sales",
                table: "Returns",
                columns: new[] { "TenantId", "ReturnNumber" },
                unique: true,
                filter: "[ReturnNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SaleDiscounts_TenantId_SaleId_SaleItemId",
                schema: "sales",
                table: "SaleDiscounts",
                columns: new[] { "TenantId", "SaleId", "SaleItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_TenantId_SaleId_ItemNumber",
                schema: "sales",
                table: "SaleItems",
                columns: new[] { "TenantId", "SaleId", "ItemNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalePayments_TenantId_IdempotencyKey",
                schema: "sales",
                table: "SalePayments",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalePayments_TenantId_PaymentMethodId",
                schema: "sales",
                table: "SalePayments",
                columns: new[] { "TenantId", "PaymentMethodId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalePayments_TenantId_SaleId_Status",
                schema: "sales",
                table: "SalePayments",
                columns: new[] { "TenantId", "SaleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_BranchId_CreatedAt",
                schema: "sales",
                table: "Sales",
                columns: new[] { "TenantId", "BranchId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_CustomerId",
                schema: "sales",
                table: "Sales",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_IdempotencyKey",
                schema: "sales",
                table: "Sales",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_ReceiptNumber",
                schema: "sales",
                table: "Sales",
                columns: new[] { "TenantId", "ReceiptNumber" },
                unique: true,
                filter: "[ReceiptNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_ShiftId",
                schema: "sales",
                table: "Sales",
                columns: new[] { "TenantId", "ShiftId" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_Status_CreatedAt",
                schema: "sales",
                table: "Sales",
                columns: new[] { "TenantId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_StockReservationId",
                schema: "sales",
                table: "Sales",
                columns: new[] { "TenantId", "StockReservationId" },
                unique: true,
                filter: "[StockReservationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SaleStatusHistory_TenantId_SaleId_ChangedAt",
                schema: "sales",
                table: "SaleStatusHistory",
                columns: new[] { "TenantId", "SaleId", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceiptSequences",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "RefundPayments",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "ReturnItems",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SaleDiscounts",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalePayments",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SaleStatusHistory",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "Returns",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SaleItems",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PaymentMethods",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "Sales",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "CashierShifts",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "sales");
        }
    }
}
