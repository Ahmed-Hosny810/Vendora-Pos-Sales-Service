
using Microsoft.EntityFrameworkCore;
using Pos.SalesService.Application.DTOS.ReadModelsDTOs;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Infrastructure.Persistence.Contexts;
using Pos.SalesService.Infrastructure.Persistence.ReadModels;

namespace Pos.SalesService.Infrastructure.Persistence.Repositories
{
    public class ReadModelsRepositoryAsync : IReadModelsRepositoryAsync
    {
        private readonly ApplicationDbContext _context;

        public ReadModelsRepositoryAsync(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<BranchReadModelDto?> GetReceiptInfoAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken)
        {
            return await _context.Set<SalesBranchReadModel>()
                .Where(b => b.TenantId == tenantId && b.Id == branchId)
                .Select(b => new BranchReadModelDto
                {
                    Id = b.Id,
                    TenantId = b.TenantId,
                    NameAr = b.NameAr,
                    NameEn = b.NameEn,
                    Status = b.Status,
                    ReceiptHeader = b.ReceiptHeader
                })
                .SingleOrDefaultAsync(cancellationToken);
        }
    }
}
