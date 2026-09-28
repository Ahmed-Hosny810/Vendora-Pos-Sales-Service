using AutoMapper;
using MediatR;
using Pos.SalesService.Application.Features.CashierShifts.DTOs;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
namespace Pos.SalesService.Application.Features.CashierShifts.Queries.GetByIdQuery;

public class GetCashierShiftByIdQuery : IRequest<Result<CashierShiftDto>>
{
    public Guid ShiftId { get; set; }
}

public class GetCashierShiftByIdQueryHandler : IRequestHandler<GetCashierShiftByIdQuery, Result<CashierShiftDto>>
{
    private readonly ICashierShiftRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    public GetCashierShiftByIdQueryHandler(ICashierShiftRepositoryAsync repository,
        ICurrentUserService currentUserService, IMapper mapper)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<Result<CashierShiftDto>> Handle(GetCashierShiftByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            throw new UnauthorizedAccessException("A valid tenant is required.");
        var shift = await _repository.GetCashierShiftByIdAsync(tenantId.Value, request.ShiftId, cancellationToken);
        return shift == null ? Result<CashierShiftDto>.Failure("Cashier shift was not found.")
            : Result<CashierShiftDto>.Success(_mapper.Map<CashierShiftDto>(shift));
    }
}
