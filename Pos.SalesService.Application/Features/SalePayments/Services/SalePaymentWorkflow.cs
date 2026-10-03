using Pos.SalesService.Application.DTOS.InventoryClient;
using Pos.SalesService.Application.Features.Sales.DTOs.Calculations;
using Pos.SalesService.Application.Interfaces.Clients;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Features.SalePayments.Services;

/// <summary>
/// Shared checks and calculations for sale-payment commands (record, confirm, cancel).
/// Handlers stage the operation; the unit of work in the calling handler saves it.
/// </summary>
public class SalePaymentWorkflow
{
    private readonly ICurrentUserService _currentUser;
    private readonly ISalePaymentRepositoryAsync _repository;
    private readonly IInventoryClient _inventoryClient;
    private readonly ISaleCalculationService _calculator;

    public SalePaymentWorkflow(
        ICurrentUserService currentUser,
        ISalePaymentRepositoryAsync repository,
        IInventoryClient inventoryClient,
        ISaleCalculationService calculator)
    {
        _currentUser = currentUser;
        _repository = repository;
        _inventoryClient = inventoryClient;
        _calculator = calculator;
    }

    // ── Authorization & loading ─────────────────────────────────────────

    /// <summary>Confirms the current user is an authenticated tenant member.</summary>
    public Result ValidateActor()
    {
        var hasValidIdentity =
            _currentUser.TenantId.HasValue &&
            _currentUser.TenantId != Guid.Empty &&
            Guid.TryParse(_currentUser.UserId, out var userId) &&
            userId != Guid.Empty;

        if (!hasValidIdentity)
            return Result.Failure("A valid tenant and user are required.");

        return Result.Success();
    }

    /// <summary>Loads the sale for payment and confirms it belongs to the current cashier's shift.</summary>
    public async Task<Result<Sale>> LoadSaleAsync(Guid saleId, CancellationToken cancellationToken)
    {
        var actorResult = ValidateActor();

        if (actorResult.IsFailure)
            return Result<Sale>.Failure(actorResult.Errors.ToArray());

        var sale = await _repository.GetSaleForPaymentAsync(
            _currentUser.TenantId!.Value, saleId, cancellationToken);

        if (sale == null)
            return Result<Sale>.Failure("Sale was not found.");

        // A cashier handles payments only in their own session.
        var currentUserId = Guid.Parse(_currentUser.UserId!);

        if (sale.Shift.CashierUserId != currentUserId)
            return Result<Sale>.Failure("This sale belongs to another cashier's shift.");

        return Result<Sale>.Success(sale);
    }

    // ── State validation ────────────────────────────────────────────────

    /// <summary>Confirms the sale is in a payable state and the caller's view of it isn't stale.</summary>
    public Result ValidateState(Sale sale, byte[] rowVersion)
    {
        if (sale.Status != SaleStatus.PendingPayment)
            return Result.Failure("The sale must be pending payment.");

        if (sale.Shift.Status != CashierShiftStatus.Open)
            return Result.Failure("An open shift is required.");

        if (!sale.RowVersion.SequenceEqual(rowVersion))
            return Result.Failure("This sale changed. Reload it and try again.");

        return Result.Success();
    }

    /// <summary>
    /// Confirms the sale's stock reservation is still active and still matches its
    /// tracked items. A sale with no tracked items should have no reservation at all.
    /// </summary>
    public async Task<Result> ValidateReservationAsync(Sale sale, CancellationToken cancellationToken)
    {
        var trackedItems = sale.Items.Where(i => i.TrackInventorySnapshot).ToList();

        if (trackedItems.Count == 0)
        {
            return sale.StockReservationId.HasValue
                ? Result.Failure("An unexpected stock reservation requires review.")
                : Result.Success();
        }

        if (!sale.StockReservationId.HasValue)
            return Result.Failure("A stock reservation is required before payment.");

        var response = await _inventoryClient.GetStockReservationByIdAsync(
            sale.StockReservationId.Value, cancellationToken);

        if (response.IsFailure)
            return Result.Failure(response.Errors.ToArray());

        var reservation = response.Value;

        var reservationIsValid =
            reservation != null &&
            reservation.Id == sale.StockReservationId &&
            reservation.ReferenceId == sale.Id &&
            reservation.BranchId == sale.BranchId &&
            reservation.Status == "Active" &&
            reservation.ExpiresAt > DateTime.UtcNow;

        if (!reservationIsValid)
            return Result.Failure("The reservation is not active. Recover checkout before taking payment.");

        if (!ReservedItemsMatch(reservation!.Items, trackedItems))
            return Result.Failure("Reserved quantities do not match the sale.");

        return Result.Success();
    }

    private static bool ReservedItemsMatch(
        IReadOnlyList<StockReservationItemDetailsDto>? reservedItems, IReadOnlyList<SaleItem> trackedItems)
    {
        if (reservedItems == null || reservedItems.Count != trackedItems.Count)
            return false;

        return trackedItems.All(item =>
            reservedItems.Count(r =>
                r.ProductId == item.ProductId &&
                r.ProductVariantId == item.ProductVariantId &&
                r.Quantity == item.Quantity) == 1);
    }

    // ── Totals calculation ──────────────────────────────────────────────

    /// <summary>Calculates settlement totals (paid, change, remaining due) from the sale's current payments.</summary>
    public Result<PaymentCalculationResult> Calculate(Sale sale)
    {
        var input = new PaymentCalculationInput
        {
            SaleTotal = sale.Total,
            Payments = sale.Payments.Select(p => new PaymentCalculationItemInput
            {
                PaymentId = p.Id,
                Status = p.Status,
                IsCash = p.IsCashSnapshot,
                Amount = p.Amount,
                ChangeAmount = p.ChangeAmount
            }).ToList()
        };

        return _calculator.CalculatePayments(input);
    }

    /// <summary>Recalculates and writes the sale's PaidAmount/ChangeAmount, marking it for save.</summary>
    public Result UpdateTotals(Sale sale)
    {
        var calculation = Calculate(sale);

        if (calculation.IsFailure)
            return Result.Failure(calculation.Errors.ToArray());

        sale.PaidAmount = calculation.Value!.PaidAmount;
        sale.ChangeAmount = calculation.Value.ChangeAmount;
        _repository.TouchSale(sale);

        return Result.Success();
    }
}