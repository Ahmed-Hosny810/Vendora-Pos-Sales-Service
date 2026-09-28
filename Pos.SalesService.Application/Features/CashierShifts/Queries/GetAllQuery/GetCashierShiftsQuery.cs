using AutoMapper;
using MediatR;
using Pos.SalesService.Application.Features.CashierShifts.DTOs;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
namespace Pos.SalesService.Application.Features.CashierShifts.Queries.GetAllQuery;

public class GetCashierShiftsQuery : IRequest<PagedResponse<IEnumerable<CashierShiftDto>>>
{
    public GetCashierShiftsQueryParameter Parameter { get; set; } = new();
}

public class GetCashierShiftsQueryHandler : IRequestHandler<GetCashierShiftsQuery, PagedResponse<IEnumerable<CashierShiftDto>>>
{
    private readonly ICashierShiftRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    public GetCashierShiftsQueryHandler(ICashierShiftRepositoryAsync repository,
        ICurrentUserService currentUserService, IMapper mapper)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<PagedResponse<IEnumerable<CashierShiftDto>>> Handle(GetCashierShiftsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            throw new UnauthorizedAccessException("A valid tenant is required.");
        var p = request.Parameter;
        var shifts = await _repository.GetCashierShiftsPagedAsync(tenantId.Value,
            p.Filter, p.OrderKey, p.OrderDescending, p.PageNumber, p.PageSize, cancellationToken);
        return new PagedResponse<IEnumerable<CashierShiftDto>>(
            _mapper.Map<IEnumerable<CashierShiftDto>>(shifts.Data),
            shifts.PageNumber, shifts.PageSize, shifts.TotalCount);
    }
}
