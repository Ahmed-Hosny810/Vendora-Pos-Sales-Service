using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.SalesService.Infrastructure.Persistence.Migrations;

public partial class AddSaleReturnItemTaxAmount : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "TaxAmount",
            schema: "sales",
            table: "ReturnItems",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "TaxAmount", schema: "sales", table: "ReturnItems");
    }
}
