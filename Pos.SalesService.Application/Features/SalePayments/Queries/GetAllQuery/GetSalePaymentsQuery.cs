using AutoMapper;
using MediatR;
using Pos.SalesService.Application.Features.SalePayments.DTOs;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;

namespace Pos.SalesService.Application.Features.SalePayments.Queries.GetAllQuery;

public class GetSalePaymentsQuery : IRequest<Result<PagedResponse<IEnumerable<SalePaymentDto>>>>
{
    public Guid SaleId { get; set; }
    public GetSalePaymentsQueryParameter Parameter { get; set; } = new();
}
public class GetSalePaymentsQueryHandler : IRequestHandler<GetSalePaymentsQuery, Result<PagedResponse<IEnumerable<SalePaymentDto>>>>
{
    private readonly ISalePaymentRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;
    public GetSalePaymentsQueryHandler(ISalePaymentRepositoryAsync repository, ICurrentUserService currentUser, IMapper mapper)
    {
        _repository = repository; _currentUser = currentUser; _mapper = mapper;
    }

    public async Task<Result<PagedResponse<IEnumerable<SalePaymentDto>>>> Handle(
        GetSalePaymentsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        if (!tenantId.HasValue || tenantId == Guid.Empty)
            return Result<PagedResponse<IEnumerable<SalePaymentDto>>>.Failure("A valid tenant is required.");

        var sale = await _repository.GetSaleForPaymentAsync(tenantId.Value, request.SaleId, cancellationToken);

        if (sale == null)
            return Result<PagedResponse<IEnumerable<SalePaymentDto>>>.Failure("Sale was not found.");

        var parameter = request.Parameter;

        var pagedPayments = await _repository.GetSalePaymentsPagedAsync(tenantId.Value, request.SaleId,
            parameter.Filter, parameter.OrderKey, parameter.OrderDescending, parameter.PageNumber, parameter.PageSize, cancellationToken);

        return Result<PagedResponse<IEnumerable<SalePaymentDto>>>.Success(
            new PagedResponse<IEnumerable<SalePaymentDto>>(_mapper.Map<IEnumerable<SalePaymentDto>>(pagedPayments.Data),
                pagedPayments.PageNumber, pagedPayments.PageSize, pagedPayments.TotalCount));
    }
}

