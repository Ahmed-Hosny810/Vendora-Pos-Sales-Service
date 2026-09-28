using AutoMapper;
using MediatR;
using Pos.SalesService.Application.Features.Customers.DTOs;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using PhoneNumbers;

namespace Pos.SalesService.Application.Features.Customers.Queries.GetByPhoneQuery;

public class GetCustomerByPhoneQuery : IRequest<Result<CustomerDto>>
{
    public string Phone { get; set; } = string.Empty;
}

public class GetCustomerByPhoneQueryHandler : IRequestHandler<GetCustomerByPhoneQuery, Result<CustomerDto>>
{
    private readonly ICustomerRepositoryAsync _customerRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetCustomerByPhoneQueryHandler(ICustomerRepositoryAsync customerRepository,
        ICurrentUserService currentUserService, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<Result<CustomerDto>> Handle(GetCustomerByPhoneQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            throw new UnauthorizedAccessException("A valid tenant is required.");

        var phoneUtil = PhoneNumberUtil.GetInstance();
        string normalizedPhone;
        try
        {
            var parsedPhone = phoneUtil.Parse(request.Phone, "EG");

            if (!phoneUtil.IsValidNumber(parsedPhone) || parsedPhone.HasExtension)
                return Result<CustomerDto>.Failure("Enter a valid phone number without an extension.");

            normalizedPhone = phoneUtil.Format(parsedPhone, PhoneNumberFormat.E164);
        }
        catch (NumberParseException)
        {
            return Result<CustomerDto>.Failure("Enter a valid phone number.");
        }

        var customer = await _customerRepository.GetCustomerByPhoneAsync(
            tenantId.Value, normalizedPhone, cancellationToken);

        return customer == null
            ? Result<CustomerDto>.Failure("Customer was not found.")
            : Result<CustomerDto>.Success(_mapper.Map<CustomerDto>(customer));
    }
}
