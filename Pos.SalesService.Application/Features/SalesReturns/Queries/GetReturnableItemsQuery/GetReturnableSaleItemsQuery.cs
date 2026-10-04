using MediatR;
using Pos.SalesService.Application.Features.SalesReturns.DTOs;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;

namespace Pos.SalesService.Application.Features.SalesReturns.Queries.GetReturnableItemsQuery;

public class GetReturnableSaleItemsQuery : IRequest<Result<IEnumerable<ReturnableSaleItemDto>>>
{
    public Guid SaleId { get; set; }
}

public class GetReturnableSaleItemsQueryHandler : IRequestHandler<GetReturnableSaleItemsQuery, Result<IEnumerable<ReturnableSaleItemDto>>>
{
    private readonly ISaleReturnRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUser;
    public GetReturnableSaleItemsQueryHandler(ISaleReturnRepositoryAsync repository, ICurrentUserService currentUser)
    {
        _repository = repository; _currentUser = currentUser;
    }

    public async Task<Result<IEnumerable<ReturnableSaleItemDto>>> Handle(GetReturnableSaleItemsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            return Result<IEnumerable<ReturnableSaleItemDto>>.Failure("A valid tenant is required.");

        var sale = await _repository.GetReturnableSaleItemsAsync(tenantId.Value, request.SaleId, cancellationToken);

        if (sale == null)
            return Result<IEnumerable<ReturnableSaleItemDto>>.Failure("Sale was not found.");

        if (sale.Status != SaleStatus.Completed && sale.Status != SaleStatus.PartiallyReturned && sale.Status != SaleStatus.Returned)
            return Result<IEnumerable<ReturnableSaleItemDto>>.Failure("Only issued sales have returnable items.");

        return Result<IEnumerable<ReturnableSaleItemDto>>.Success(sale.Items);
    }
}
