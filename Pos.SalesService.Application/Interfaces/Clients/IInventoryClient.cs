
using Pos.SalesService.Application.DTOS.InventoryClient;
using Pos.SalesService.Application.Wrappers;

namespace Pos.SalesService.Application.Interfaces.Clients
{
    public interface IInventoryClient
    {
        Task<Result<Guid>> CreateStockReservationAsync(CreateStockReservationRequest request,CancellationToken cancellationToken);

        Task<Result<StockReservationDetailsDto>> GetStockReservationByIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken);

        Task<Result<StockReservationDetailsDto>> GetStockReservationByReferenceIdAsync(
        Guid saleId,
        CancellationToken cancellationToken);

        Task<Result> ConsumeStockReservationAsync(
            Guid reservationId,
            CancellationToken cancellationToken);

        Task<Result> ReleaseStockReservationAsync(
            Guid reservationId,
            CancellationToken cancellationToken);
    }
}
