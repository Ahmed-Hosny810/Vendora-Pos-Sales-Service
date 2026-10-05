using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Domain.Models;
using Pos.SalesService.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
namespace Pos.SalesService.Infrastructure.Persistence.Repositories
{
    public class OutboxRepositoryAsync : GenericRepositoryAsync<OutboxMessage, Guid>, IOutboxRepositoryAsync
    {
        private readonly ApplicationDbContext _context;

        public OutboxRepositoryAsync(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.OutboxMessages
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int batchSize, CancellationToken cancellationToken)
        {
            return await _context.OutboxMessages
                .Where(x => x.PublishedAt == null)
                .OrderBy(x => x.OccurredAt)
                .ThenBy(x => x.Id)
                .Take(batchSize)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        }
    }
}
