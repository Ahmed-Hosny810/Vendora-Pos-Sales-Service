using Pos.SalesService.Application.Wrappers;

namespace Pos.SalesService.Application.Interfaces
{
    public interface IUnitOfWork
    {
        Task<Result> TrySaveReturnChangesAsync(CancellationToken cancellationToken = default);
        Task<Result> TrySavePaymentChangesAsync(CancellationToken cancellationToken = default);
       Task<Result> TrySaveRefundChangesAsync(CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
