using MediatR;
using PhoneNumbers;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;

namespace Pos.SalesService.Application.Features.Customers.Commands.UpdateCommand;

public class UpdateCustomerCommand : IRequest<Result<Guid>>
{
    public Guid CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? AddressLine { get; set; }
    public string? Area { get; set; }
    public string? City { get; set; }
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, Result<Guid>>
{
    private readonly ICustomerRepositoryAsync _customerRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerCommandHandler(ICustomerRepositoryAsync customerRepository,
        ICurrentUserService currentUserService, IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {

        var tenantId = _currentUserService.TenantId;

        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            throw new UnauthorizedAccessException("A valid tenant is required.");

        var customer = await _customerRepository.GetCustomerByIdAsync(
            tenantId.Value, request.CustomerId, cancellationToken);

        if (customer == null)
            return Result<Guid>.Failure("Customer was not found.");

        if (!customer.RowVersion.SequenceEqual(request.RowVersion))
            return Result<Guid>.Failure("This customer has changed. Reload it before editing.");

        var phoneUtil = PhoneNumberUtil.GetInstance();

        string normalizedPhone;

        try
        {
            var parsed = phoneUtil.Parse(request.Phone, "EG");
            if (!phoneUtil.IsValidNumber(parsed) || parsed.HasExtension)
                return Result<Guid>.Failure("Enter a valid phone number without an extension.");
            normalizedPhone = phoneUtil.Format(parsed, PhoneNumberFormat.E164);
        }
        catch (NumberParseException)
        {
            return Result<Guid>.Failure("Enter a valid phone number.");
        }

       
        var phoneOwner = await _customerRepository.GetCustomerByPhoneAsync(
            tenantId.Value, normalizedPhone, cancellationToken);

        if (phoneOwner != null && phoneOwner.Id != customer.Id)
            return Result<Guid>.Failure("A customer with this phone number already exists.");

        customer.FullName = request.FullName.Trim();
        customer.Phone = normalizedPhone;
        customer.AddressLine = request.AddressLine?.Trim();
        customer.Area = request.Area?.Trim();
        customer.City = request.City?.Trim();
        customer.Notes = request.Notes?.Trim();
        customer.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
      
        return Result<Guid>.Success(customer.Id);
    }
}
