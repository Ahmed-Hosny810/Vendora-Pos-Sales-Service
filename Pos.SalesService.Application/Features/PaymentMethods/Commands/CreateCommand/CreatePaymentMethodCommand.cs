using MediatR;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Exceptions;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Features.PaymentMethods.Commands.CreateCommand;

public class CreatePaymentMethodCommand : IRequest<Result<Guid>>
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsCash { get; set; }
    public bool RequiresReferenceNumber { get; set; }
    public int SortOrder { get; set; }
}

public class CreatePaymentMethodCommandHandler : IRequestHandler<CreatePaymentMethodCommand, Result<Guid>>
{
    private readonly IPaymentMethodRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    public CreatePaymentMethodCommandHandler(IPaymentMethodRepositoryAsync repository,
        ICurrentUserService currentUserService, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        
        var tenantId = _currentUserService.TenantId;

        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            throw new UnauthorizedAccessException("A valid tenant is required.");

        var code = request.Code.Trim().ToUpperInvariant();

        var paymentMethod = await _repository.GetPaymentMethodByCodeAsync(
            tenantId.Value, code, cancellationToken);

        if (paymentMethod != null)
            return Result<Guid>.Failure("A payment method with this code already exists.");

        var newMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Name = request.Name.Trim(),
            Code = code,
            IsCash = request.IsCash,
            RequiresReferenceNumber = request.RequiresReferenceNumber,
            SortOrder = request.SortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(newMethod, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicatePaymentMethodCodeException)
        {
            return Result<Guid>.Failure("A payment method with this code already exists.");
        }
        return Result<Guid>.Success(newMethod.Id);
    }
}
