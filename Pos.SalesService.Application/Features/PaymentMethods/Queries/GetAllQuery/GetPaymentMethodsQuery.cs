using MediatR;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using AutoMapper;
using Pos.SalesService.Application.Features.PaymentMethods.DTOs;
namespace Pos.SalesService.Application.Features.PaymentMethods.Queries.GetAllQuery;
public class GetPaymentMethodsQuery : IRequest<PagedResponse<IEnumerable<PaymentMethodDto>>>
{
    public GetPaymentMethodsQueryParameter Parameter { get; set; } = new();
}
public class GetPaymentMethodsQueryHandler : IRequestHandler<GetPaymentMethodsQuery, PagedResponse<IEnumerable<PaymentMethodDto>>>
{
    private readonly IPaymentMethodRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    public GetPaymentMethodsQueryHandler(IPaymentMethodRepositoryAsync repository,
        ICurrentUserService currentUserService, IMapper mapper)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }
    public async Task<PagedResponse<IEnumerable<PaymentMethodDto>>> Handle(GetPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            throw new UnauthorizedAccessException("A valid tenant is required.");

        var p = request.Parameter;

        var methods = await _repository.GetPaymentMethodsPagedAsync(tenantId.Value,
            p.Filter, p.OrderKey, p.OrderDescending, p.PageNumber, p.PageSize, cancellationToken);

        return new PagedResponse<IEnumerable<PaymentMethodDto>>(
            _mapper.Map<IEnumerable<PaymentMethodDto>>(methods.Data),
            methods.PageNumber, methods.PageSize, methods.TotalCount);
    }
}
