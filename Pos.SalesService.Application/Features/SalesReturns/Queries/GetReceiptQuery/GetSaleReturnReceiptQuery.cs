using MediatR;
using Pos.SalesService.Application.Features.SalesReturns.DTOs.Receipts;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;

namespace Pos.SalesService.Application.Features.SalesReturns.Queries.GetReceiptQuery;

public class GetSaleReturnReceiptQuery : IRequest<Result<SaleReturnReceiptDto>>
{
    public Guid ReturnId { get; set; }
}

public class GetSaleReturnReceiptQueryHandler
    : IRequestHandler<GetSaleReturnReceiptQuery, Result<SaleReturnReceiptDto>>
{
    private readonly ISaleReturnRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUser;

    public GetSaleReturnReceiptQueryHandler(ISaleReturnRepositoryAsync repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<SaleReturnReceiptDto>> Handle(
        GetSaleReturnReceiptQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            return Result<SaleReturnReceiptDto>.Failure("A valid tenant is required.");

        var receipt = await _repository.GetSaleReturnReceiptAsync(
            tenantId.Value, request.ReturnId, cancellationToken);

        return receipt == null
            ? Result<SaleReturnReceiptDto>.Failure("Sale return receipt was not found.")
            : Result<SaleReturnReceiptDto>.Success(receipt);
    }
}

