

namespace Pos.SalesService.Application.Interfaces
{
    public interface IUnitOfWork
    {
        Task<Pos.SalesService.Application.Wrappers.Result> TrySavePaymentChangesAsync(CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
