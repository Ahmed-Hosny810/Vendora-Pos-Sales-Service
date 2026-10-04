using MediatR;
using Pos.SalesService.Application.Exceptions;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;
using Pos.SalesService.Application.Features.SalesReturns.DTOs;
using Pos.SalesService.Application.Features.SalesReturns.Services;

namespace Pos.SalesService.Application.Features.SalesReturns.Commands.UpdateReturnDraftCommand;

public class UpdateSaleReturnDraftCommand : IRequest<Result<Guid>>
{
    public Guid ReturnId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public string Reason { get; set; } = string.Empty;
    public List<SaleReturnItemInput> Items { get; set; } = new();
}

public class UpdateSaleReturnDraftCommandHandler : IRequestHandler<UpdateSaleReturnDraftCommand, Result<Guid>>
{
    private readonly ISaleReturnRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SaleReturnService _draftService;

    public UpdateSaleReturnDraftCommandHandler(ISaleReturnRepositoryAsync repository,
        ICurrentUserService currentUser, IUnitOfWork unitOfWork, SaleReturnService draftService)
    {
        _repository = repository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _draftService = draftService;
    }

    public async Task<Result<Guid>> Handle(UpdateSaleReturnDraftCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        if (!tenantId.HasValue || tenantId.Value == Guid.Empty ||
            !Guid.TryParse(_currentUser.UserId, out var userId) || userId == Guid.Empty)
            return Result<Guid>.Failure("A valid authenticated tenant user is required.");

        var saleReturn = await _repository.GetByIdAsync(tenantId.Value, request.ReturnId, cancellationToken);

        if (saleReturn == null)
            return Result<Guid>.Failure("Sale return was not found.");

        if (saleReturn.Status != SaleReturnStatus.Draft)
            return Result<Guid>.Failure("Only draft returns can be edited.");

        if (!saleReturn.RowVersion.SequenceEqual(request.RowVersion))
            return Result<Guid>.Failure("This return has changed. Reload it and try again.");

        var prepared = await _draftService.PrepareAsync(tenantId.Value, saleReturn.OriginalSaleId,
            saleReturn.Id, request.Items, cancellationToken);

        if (prepared.IsFailure)
            return Result<Guid>.Failure(prepared.Errors.ToArray());

        _repository.ReplaceItems(saleReturn, prepared.Value!.Items.ToList());
        saleReturn.Reason = request.Reason.Trim();
        saleReturn.RefundAmount = prepared.Value.RefundAmount;
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

