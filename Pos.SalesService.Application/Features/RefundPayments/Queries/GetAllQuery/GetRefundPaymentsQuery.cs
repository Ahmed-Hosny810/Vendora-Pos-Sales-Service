using AutoMapper;
using MediatR;
using Pos.SalesService.Application.Features.RefundPayments.DTOs;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;

namespace Pos.SalesService.Application.Features.RefundPayments.Queries.GetAllQuery;

public class GetRefundPaymentsQuery : IRequest<Result<PagedResponse<IEnumerable<RefundPaymentDto>>>>
{
    public Guid ReturnId { get; set; }
    public GetRefundPaymentsQueryParameter Parameter { get; set; } = new();
}

public class GetRefundPaymentsQueryHandler
    : IRequestHandler<GetRefundPaymentsQuery, Result<PagedResponse<IEnumerable<RefundPaymentDto>>>>
{
    private readonly IRefundPaymentRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public GetRefundPaymentsQueryHandler(
        IRefundPaymentRepositoryAsync repository, ICurrentUserService currentUser, IMapper mapper)
    {
        _repository = repository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<PagedResponse<IEnumerable<RefundPaymentDto>>>> Handle(
        GetRefundPaymentsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            return Result<PagedResponse<IEnumerable<RefundPaymentDto>>>.Failure("A valid tenant is required.");

        if (!await _repository.ReturnExistsAsync(tenantId.Value, request.ReturnId, cancellationToken))
            return Result<PagedResponse<IEnumerable<RefundPaymentDto>>>.Failure("Sale return was not found.");

        var parameter = request.Parameter;

        var page = await _repository.GetRefundPaymentsPagedAsync(
            tenantId.Value, request.ReturnId, parameter.Filter, parameter.OrderKey,
            parameter.OrderDescending, parameter.PageNumber, parameter.PageSize, cancellationToken);

        return Result<PagedResponse<IEnumerable<RefundPaymentDto>>>.Success(
            new PagedResponse<IEnumerable<RefundPaymentDto>>(
                _mapper.Map<IEnumerable<RefundPaymentDto>>(page.Data),
                page.PageNumber, page.PageSize, page.TotalCount));
    }
}

