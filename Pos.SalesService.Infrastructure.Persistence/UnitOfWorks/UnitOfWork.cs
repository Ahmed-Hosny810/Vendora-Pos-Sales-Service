using Microsoft.EntityFrameworkCore;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Infrastructure.Persistence.Contexts;



namespace Pos.SalesService.Infrastructure.Persistence.UnitofWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
