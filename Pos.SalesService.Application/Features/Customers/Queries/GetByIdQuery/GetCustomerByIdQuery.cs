using AutoMapper;
using MediatR;
using Pos.SalesService.Application.Features.Customers.DTOs;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;

namespace Pos.SalesService.Application.Features.Customers.Queries.GetByIdQuery;

public class GetCustomerByIdQuery : IRequest<Result<CustomerDto>>
{
    public Guid CustomerId { get; set; }
}

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDto>>
{
    private readonly ICustomerRepositoryAsync _customerRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetCustomerByIdQueryHandler(ICustomerRepositoryAsync customerRepository,
        ICurrentUserService currentUserService, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<Result<CustomerDto>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            throw new UnauthorizedAccessException("A valid tenant is required.");

        var customer = await _customerRepository.GetCustomerByIdAsync(
            tenantId.Value, request.CustomerId, cancellationToken);

        return customer == null
            ? Result<CustomerDto>.Failure("Customer was not found.")
            : Result<CustomerDto>.Success(_mapper.Map<CustomerDto>(customer));
    }
}
