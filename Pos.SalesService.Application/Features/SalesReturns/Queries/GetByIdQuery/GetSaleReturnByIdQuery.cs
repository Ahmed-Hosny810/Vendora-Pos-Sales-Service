using AutoMapper;
using MediatR;
using Pos.SalesService.Application.Features.SalesReturns.DTOs;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;

namespace Pos.SalesService.Application.Features.SalesReturns.Queries.GetByIdQuery;

public class GetSaleReturnByIdQuery : IRequest<Result<SaleReturnDetailsDto>>
{
    public Guid ReturnId { get; set; }
}

public class GetSaleReturnByIdQueryHandler : IRequestHandler<GetSaleReturnByIdQuery, Result<SaleReturnDetailsDto>>
{
    private readonly ISaleReturnRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;
    public GetSaleReturnByIdQueryHandler(ISaleReturnRepositoryAsync repository, ICurrentUserService currentUser, IMapper mapper)
    {
        _repository = repository; _currentUser = currentUser; _mapper = mapper;
    }

    public async Task<Result<SaleReturnDetailsDto>> Handle(GetSaleReturnByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            return Result<SaleReturnDetailsDto>.Failure("A valid tenant is required.");
        var saleReturn = await _repository.GetByIdAsync(tenantId.Value, request.ReturnId, cancellationToken);
        if (saleReturn == null)
            return Result<SaleReturnDetailsDto>.Failure("Sale return was not found.");
        return Result<SaleReturnDetailsDto>.Success(_mapper.Map<SaleReturnDetailsDto>(saleReturn));
    }
}

