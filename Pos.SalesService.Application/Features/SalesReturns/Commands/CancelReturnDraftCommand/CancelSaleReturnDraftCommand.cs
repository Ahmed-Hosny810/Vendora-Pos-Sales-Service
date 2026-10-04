using MediatR;
using Pos.SalesService.Application.Exceptions;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;

namespace Pos.SalesService.Application.Features.SalesReturns.Commands.CancelReturnDraftCommand;

public class CancelSaleReturnDraftCommand : IRequest<Result<Guid>>
{
    public Guid ReturnId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class CancelSaleReturnDraftCommandHandler : IRequestHandler<CancelSaleReturnDraftCommand, Result<Guid>>
{
    private readonly ISaleReturnRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CancelSaleReturnDraftCommandHandler(ISaleReturnRepositoryAsync repository,
        ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CancelSaleReturnDraftCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty ||
            !Guid.TryParse(_currentUser.UserId, out var userId) || userId == Guid.Empty)
            return Result<Guid>.Failure("A valid authenticated tenant user is required.");

        var saleReturn = await _repository.GetByIdAsync(tenantId.Value, request.ReturnId, cancellationToken);

        if (saleReturn == null)
            return Result<Guid>.Failure("Sale return was not found.");
        if (saleReturn.Status != SaleReturnStatus.Draft)
            return Result<Guid>.Failure("Only draft returns can be cancelled.");
        if (!saleReturn.RowVersion.SequenceEqual(request.RowVersion))
            return Result<Guid>.Failure("This return has changed. Reload it and try again.");

        saleReturn.Status = SaleReturnStatus.Cancelled;
        saleReturn.CancelledAt = DateTime.UtcNow;
        _repository.MarkUpdated(saleReturn);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result<Guid>.Failure("The return changed while saving. Reload it and try again.");
        }

        return Result<Guid>.Success(saleReturn.Id);
    }
}

