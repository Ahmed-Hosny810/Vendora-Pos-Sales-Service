using AutoMapper;
using MediatR;
using Pos.SalesService.Application.Features.Sales.DTOs;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
namespace Pos.SalesService.Application.Features.Sales.Queries.GetStatusHistoryQuery;

public class GetSaleStatusHistoryQuery : IRequest<Result<PagedResponse<IEnumerable<SaleStatusHistoryDto>>>>
{
    public Guid SaleId { get; set; }
    public GetSaleStatusHistoryQueryParameter Parameter { get; set; } = new();
}
public class GetSaleStatusHistoryQueryHandler : IRequestHandler<GetSaleStatusHistoryQuery, Result<PagedResponse<IEnumerable<SaleStatusHistoryDto>>>>
{
    private readonly ISaleRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;
    public GetSaleStatusHistoryQueryHandler(ISaleRepositoryAsync repository, ICurrentUserService currentUser, IMapper mapper)
    {
        _repository = repository; _currentUser = currentUser; _mapper = mapper;
    }
    public async Task<Result<PagedResponse<IEnumerable<SaleStatusHistoryDto>>>> Handle(GetSaleStatusHistoryQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        if (!tenantId.HasValue || tenantId == Guid.Empty)
            return Result<PagedResponse<IEnumerable<SaleStatusHistoryDto>>>.Failure("A valid tenant is required.");

        if (!await _repository.ExistsAsync(tenantId.Value, request.SaleId, cancellationToken))
            return Result<PagedResponse<IEnumerable<SaleStatusHistoryDto>>>.Failure("Sale was not found.");

        var p = request.Parameter;
        var salesStatusHistories = await _repository.GetSaleStatusHistoryPagedAsync(tenantId.Value, request.SaleId, 
            p.Filter, p.OrderKey, p.OrderDescending, p.PageNumber, p.PageSize, cancellationToken);

        return Result<PagedResponse<IEnumerable<SaleStatusHistoryDto>>>.Success(
            new PagedResponse<IEnumerable<SaleStatusHistoryDto>>(_mapper.Map<IEnumerable<SaleStatusHistoryDto>>(salesStatusHistories.Data),
                salesStatusHistories.PageNumber, salesStatusHistories.PageSize, salesStatusHistories.TotalCount));
    }
}

