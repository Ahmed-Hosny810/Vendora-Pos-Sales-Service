using AutoMapper;
using MediatR;
using Pos.SalesService.Application.Features.Sales.DTOs;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
namespace Pos.SalesService.Application.Features.Sales.Queries.GetSalesQuery;

public class GetSalesQuery : IRequest<Result<PagedResponse<IEnumerable<SaleSummaryDto>>>>
{
    
    public GetSalesQueryParameter Parameter { get; set; } = new();
}
public class GetSalesQueryHandler : IRequestHandler<GetSalesQuery, Result<PagedResponse<IEnumerable<SaleSummaryDto>>>>
{
    private readonly ISaleRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;
    public GetSalesQueryHandler(ISaleRepositoryAsync repository, ICurrentUserService currentUser, IMapper mapper)
    {
        _repository = repository; _currentUser = currentUser; _mapper = mapper;
    }
    public async Task<Result<PagedResponse<IEnumerable<SaleSummaryDto>>>> Handle(GetSalesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        if (!tenantId.HasValue || tenantId == Guid.Empty)
            return Result<PagedResponse<IEnumerable<SaleSummaryDto>>>.Failure("A valid tenant is required.");
        
        var p = request.Parameter;


        var sales = await _repository.GetSalesPagedAsync(tenantId.Value, 
            p.Filter, p.OrderKey, p.OrderDescending, p.PageNumber, p.PageSize, cancellationToken);

        return Result<PagedResponse<IEnumerable<SaleSummaryDto>>>.Success(
            new PagedResponse<IEnumerable<SaleSummaryDto>>(_mapper.Map<IEnumerable<SaleSummaryDto>>(sales.Data),
                sales.PageNumber, sales.PageSize, sales.TotalCount));
    }
}

