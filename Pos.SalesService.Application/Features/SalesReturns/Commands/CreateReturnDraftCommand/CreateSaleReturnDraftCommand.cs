using MediatR;
using Pos.SalesService.Application.Exceptions;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;
using Pos.SalesService.Application.Features.SalesReturns.DTOs;
using Pos.SalesService.Application.Features.SalesReturns.Services;

namespace Pos.SalesService.Application.Features.SalesReturns.Commands.CreateReturnDraftCommand;

public class CreateSaleReturnDraftCommand : IRequest<Result<Guid>>
{
    public Guid OriginalSaleId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<SaleReturnItemInput> Items { get; set; } = new();
}

public class CreateSaleReturnDraftCommandHandler : IRequestHandler<CreateSaleReturnDraftCommand, Result<Guid>>
{
    private readonly ISaleReturnRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SaleReturnService _saleReturntService;

    public CreateSaleReturnDraftCommandHandler(ISaleReturnRepositoryAsync repository,
        ICurrentUserService currentUser, IUnitOfWork unitOfWork, SaleReturnService draftService)
    {
        _repository = repository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _saleReturntService = draftService;
    }

    public async Task<Result<Guid>> Handle(CreateSaleReturnDraftCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        if (!tenantId.HasValue || tenantId.Value == Guid.Empty ||
            !Guid.TryParse(_currentUser.UserId, out var userId) || userId == Guid.Empty)
            return Result<Guid>.Failure("A valid authenticated tenant user is required.");

        var prepared = await _saleReturntService.PrepareAsync(tenantId.Value, request.OriginalSaleId,
            Guid.NewGuid(), request.Items, cancellationToken);

        if (prepared.IsFailure)
            return Result<Guid>.Failure(prepared.Errors.ToArray());

        var saleReturn = prepared.Value!;
        saleReturn.Reason = request.Reason.Trim();
        saleReturn.ProcessedByUserId = userId;
        saleReturn.Status = SaleReturnStatus.Draft;
        saleReturn.CreatedAt = DateTime.UtcNow;
        await _repository.AddAsync(saleReturn, cancellationToken);
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

