using MediatR;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Exceptions;

namespace Pos.SalesService.Application.Features.PaymentMethods.Commands.UpdateCommand;

public class UpdatePaymentMethodCommand : IRequest<Result<Guid>>
{
    public Guid PaymentMethodId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsCash { get; set; }
    public bool RequiresReferenceNumber { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class UpdatePaymentMethodCommandHandler : IRequestHandler<UpdatePaymentMethodCommand, Result<Guid>>
{
    private readonly IPaymentMethodRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    public UpdatePaymentMethodCommandHandler(IPaymentMethodRepositoryAsync repository,
        ICurrentUserService currentUserService, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(UpdatePaymentMethodCommand request, CancellationToken cancellationToken)
    {

        var tenantId = _currentUserService.TenantId;

        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            throw new UnauthorizedAccessException("A valid tenant is required.");

        var method = await _repository.GetPaymentMethodByIdAsync(
            tenantId.Value, request.PaymentMethodId, cancellationToken);

        if (method == null)
            return Result<Guid>.Failure("Payment method was not found.");

        if (!method.RowVersion.SequenceEqual(request.RowVersion))
            return Result<Guid>.Failure("This payment method has changed. Reload it before editing.");

        if (method.IsCash != request.IsCash &&await _repository.HasBeenUsedAsync(tenantId.Value, method.Id, cancellationToken))
            return Result<Guid>.Failure("Cannot change IsCash after use. Create a new payment method instead.");

        // Normalize codes so cash, CASH and surrounding whitespace are treated consistently.
        var code = request.Code.Trim().ToUpperInvariant();

        var paymentMethod = await _repository.GetPaymentMethodByCodeAsync(
            tenantId.Value, code, cancellationToken);

        if (paymentMethod != null && paymentMethod.Id != method.Id)
            return Result<Guid>.Failure("A payment method with this code already exists.");

        // Update only the method; historical payment/refund snapshots are not changed.
        method.Name = request.Name.Trim();
        method.Code = code;
        method.IsCash = request.IsCash;
        method.RequiresReferenceNumber = request.RequiresReferenceNumber;
        method.SortOrder = request.SortOrder;
        method.IsActive = request.IsActive;
        method.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result<Guid>.Failure("This payment method changed while saving. Reload it and try again.");
        }
        catch (DuplicatePaymentMethodCodeException)
        {
            return Result<Guid>.Failure("A payment method with this code already exists.");
        }
        return Result<Guid>.Success(method.Id);
    }
}
