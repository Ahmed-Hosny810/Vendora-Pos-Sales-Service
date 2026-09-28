using AutoMapper;
using MediatR;
using Pos.SalesService.Application.Features.Customers.DTOs;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;

namespace Pos.SalesService.Application.Features.Customers.Queries.GetCustomersQuery;

public class GetCustomersQuery : IRequest<PagedResponse<IEnumerable<CustomerDto>>>
{
    public GetCustomersQueryParameter Parameter { get; set; } = new();
}

public class GetCustomersQueryHandler
    : IRequestHandler<GetCustomersQuery, PagedResponse<IEnumerable<CustomerDto>>>
{
    private readonly ICustomerRepositoryAsync _customerRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetCustomersQueryHandler(ICustomerRepositoryAsync customerRepository,
        ICurrentUserService currentUserService, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<PagedResponse<IEnumerable<CustomerDto>>> Handle(
        GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            throw new UnauthorizedAccessException("A valid tenant is required.");

        var parameter = request.Parameter;

        var customers = await _customerRepository.GetCustomersPagedAsync(
            tenantId.Value, parameter.Filter, parameter.OrderKey, parameter.OrderDescending,
            parameter.PageNumber, parameter.PageSize, cancellationToken);
        

        return new PagedResponse<IEnumerable<CustomerDto>>(
            _mapper.Map<IEnumerable<CustomerDto>>(customers.Data),
            customers.PageNumber, customers.PageSize, customers.TotalCount);
    }
}
