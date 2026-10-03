using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Pos.SalesService.Application.Exceptions;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Infrastructure.Persistence.Contexts;

namespace Pos.SalesService.Infrastructure.Persistence.UnitofWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    public UnitOfWork(ApplicationDbContext context) { _context = context; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException("The record changed while saving.", exception);
        }
    }
}
