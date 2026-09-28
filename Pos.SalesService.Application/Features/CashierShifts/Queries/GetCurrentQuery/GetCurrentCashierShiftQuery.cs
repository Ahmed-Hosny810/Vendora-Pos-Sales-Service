using AutoMapper;
using MediatR;
using Pos.SalesService.Application.Features.CashierShifts.DTOs;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
namespace Pos.SalesService.Application.Features.CashierShifts.Queries.GetCurrentQuery;

public class GetCurrentCashierShiftQuery : IRequest<Result<CashierShiftDto>>
{

}

public class GetCurrentCashierShiftQueryHandler : IRequestHandler<GetCurrentCashierShiftQuery, Result<CashierShiftDto>>
{
    private readonly ICashierShiftRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    public GetCurrentCashierShiftQueryHandler(ICashierShiftRepositoryAsync repository,
        ICurrentUserService currentUserService, IMapper mapper)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<Result<CashierShiftDto>> Handle(GetCurrentCashierShiftQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            throw new UnauthorizedAccessException("A valid tenant is required.");
        if (!Guid.TryParse(_currentUserService.UserId, out var userId) || userId == Guid.Empty)
            throw new UnauthorizedAccessException("A valid user is required.");
        var shift = await _repository.GetCurrentCashierShiftAsync(tenantId.Value, userId, cancellationToken);
        return shift == null ? Result<CashierShiftDto>.Failure("No open shift was found for this cashier.")
            : Result<CashierShiftDto>.Success(_mapper.Map<CashierShiftDto>(shift));
    }
}
