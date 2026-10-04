
using Pos.SalesService.Application.DTOS.ReadModelsDTOs;

namespace Pos.SalesService.Application.Interfaces.Repositories
{
    public interface IReadModelsRepositoryAsync
    {

        Task<BranchReadModelDto?> GetReceiptInfoAsync(Guid tenantId, Guid branchId,CancellationToken cancellationToken);
    }
}
