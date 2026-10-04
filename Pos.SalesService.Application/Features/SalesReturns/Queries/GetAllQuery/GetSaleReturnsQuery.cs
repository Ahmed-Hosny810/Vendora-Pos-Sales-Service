using AutoMapper;
using MediatR;
using Pos.SalesService.Application.Features.SalesReturns.DTOs;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;

namespace Pos.SalesService.Application.Features.SalesReturns.Queries.GetAllQuery;

public class GetSaleReturnsQuery : IRequest<Result<PagedResponse<IEnumerable<SaleReturnSummaryDto>>>>
{
    public GetSaleReturnsQueryParameter Parameter { get; set; } = new();
}

public class GetSaleReturnsQueryHandler : IRequestHandler<GetSaleReturnsQuery, Result<PagedResponse<IEnumerable<SaleReturnSummaryDto>>>>
{
    private readonly ISaleReturnRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;
    public GetSaleReturnsQueryHandler(ISaleReturnRepositoryAsync repository, ICurrentUserService currentUser, IMapper mapper)
    {
        _repository = repository; _currentUser = currentUser; _mapper = mapper;
    }

    public async Task<Result<PagedResponse<IEnumerable<SaleReturnSummaryDto>>>> Handle(GetSaleReturnsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            return Result<PagedResponse<IEnumerable<SaleReturnSummaryDto>>>.Failure("A valid tenant is required.");

        var parameter = request.Parameter;

        var saleReturns = await _repository.GetPagedAsync(tenantId.Value, parameter.Filter,
            parameter.OrderKey, parameter.OrderDescending, parameter.PageNumber, parameter.PageSize, cancellationToken);

        var data = _mapper.Map<IEnumerable<SaleReturnSummaryDto>>(saleReturns.Data);

        return Result<PagedResponse<IEnumerable<SaleReturnSummaryDto>>>.Success(
            new PagedResponse<IEnumerable<SaleReturnSummaryDto>>(data, saleReturns.PageNumber, saleReturns.PageSize, saleReturns.TotalCount));
    }
}

