using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.SalesService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class updateSaleReturnEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Returns_TenantId_BranchId_CreatedAt",
                schema: "sales",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_TenantId_IdempotencyKey",
                schema: "sales",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                schema: "sales",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                schema: "sales",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "sales",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "sales",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "sales",
                table: "Returns");

            migrationBuilder.AlterColumn<Guid>(
                name: "IdempotencyKey",
                schema: "sales",
                table: "Returns",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedAt",
                schema: "sales",
                table: "Returns",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId_BranchId_CompletedAt",
                schema: "sales",
                table: "Returns",
                columns: new[] { "TenantId", "BranchId", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId_IdempotencyKey",
                schema: "sales",
                table: "Returns",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Returns_TenantId_BranchId_CompletedAt",
                schema: "sales",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_TenantId_IdempotencyKey",
                schema: "sales",
                table: "Returns");

            migrationBuilder.AlterColumn<Guid>(
                name: "IdempotencyKey",
                schema: "sales",
                table: "Returns",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedAt",
                schema: "sales",
                table: "Returns",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                schema: "sales",
                table: "Returns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                schema: "sales",
                table: "Returns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "sales",
                table: "Returns",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "sales",
                table: "Returns",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "sales",
                table: "Returns",
                type: "datetime2",
                nullable: true);

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
        }
    }
}
