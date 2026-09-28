using MediatR;
using Pos.SalesService.Application.Exceptions;
using PhoneNumbers;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Features.Customers.Commands.CreateCommand
{
    public class CreateCustomerCommand:IRequest<Result<Guid>>
    {
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? AddressLine { get; set; }
        public string? Area { get; set; }
        public string? City { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<Guid>>
    {
        private readonly ICustomerRepositoryAsync _customerRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public CreateCustomerCommandHandler(
            ICustomerRepositoryAsync customerRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _customerRepository = customerRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(
            CreateCustomerCommand request,
            CancellationToken cancellationToken)
        {

            var tenantId = _currentUserService.TenantId;

            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException(
                    "A valid tenant is required.");

            //  Validate and normalize the phone number.
            var phoneUtil = PhoneNumberUtil.GetInstance();
            string normalizedPhone;

            try
            {
                var parsedPhone = phoneUtil.Parse(request.Phone, "EG");

                if (!phoneUtil.IsValidNumber(parsedPhone) ||
                    parsedPhone.HasExtension)
                {
                    return Result<Guid>.Failure(
                        "Enter a valid phone number without an extension.");
                }

                normalizedPhone = phoneUtil.Format(
                    parsedPhone,
                    PhoneNumberFormat.E164);
            }
            catch (NumberParseException)
            {
                return Result<Guid>.Failure(
                    "Enter a valid phone number.");
            }

            var phoneExists = await _customerRepository.PhoneExistsAsync(
                tenantId.Value,
                normalizedPhone,
                cancellationToken);

            if (phoneExists)
                return Result<Guid>.Failure(
                    "A customer with this phone number already exists.");

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                FullName = request.FullName.Trim(),
                Phone = normalizedPhone,
                AddressLine = request.AddressLine?.Trim(),
                Area = request.Area?.Trim(),
                City = request.City?.Trim(),
                Notes = request.Notes?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _customerRepository.AddAsync(customer,cancellationToken);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DuplicateCustomerPhoneException)
            {
                return Result<Guid>.Failure("A customer with this phone number already exists.");
            }

            return Result<Guid>.Success(customer.Id);
        }
    }
}
