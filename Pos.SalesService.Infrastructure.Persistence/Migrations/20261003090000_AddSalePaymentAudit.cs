using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.SalesService.Infrastructure.Persistence.Migrations;

public partial class AddSalePaymentAudit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "ConfirmedByUserId", schema: "sales",
            table: "SalePayments", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "CancelledByUserId", schema: "sales",
            table: "SalePayments", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "CancelledAt", schema: "sales",
            table: "SalePayments", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<string>(name: "CancellationReason", schema: "sales",
            table: "SalePayments", type: "nvarchar(500)", maxLength: 500, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ConfirmedByUserId", schema: "sales", table: "SalePayments");
        migrationBuilder.DropColumn(name: "CancelledByUserId", schema: "sales", table: "SalePayments");
        migrationBuilder.DropColumn(name: "CancelledAt", schema: "sales", table: "SalePayments");
        migrationBuilder.DropColumn(name: "CancellationReason", schema: "sales", table: "SalePayments");
    }
}
