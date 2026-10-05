using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Interfaces.Repositories
{
    public interface IOutboxRepositoryAsync:IGenericRepositoryAsync<OutboxMessage,Guid>
    {
        Task<IReadOnlyList<Guid>> GetPendingIdsAsync(
             int batchSize,
             CancellationToken cancellationToken);

        Task<OutboxMessage?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken);
    }
}
