using MediatR;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Features.CashierShifts.Commands.OpenShiftCommand
{
    public class OpenCashierShiftCommand:IRequest<Result<Guid>>
    {
        public Guid BranchId { get; set; }
        public Guid TerminalId { get; set; }
        public decimal OpeningCash { get; set; }

    }
    public class OpenCashierShiftCommandHandler : IRequestHandler<OpenCashierShiftCommand, Result<Guid>>
    {
        private readonly ICashierShiftRepositoryAsync _cashierShiftRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISalesValidationService _salesValidationService;

        public OpenCashierShiftCommandHandler(
            ICashierShiftRepositoryAsync cashierShiftRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ISalesValidationService salesValidationService

            )
        {
            _cashierShiftRepository = cashierShiftRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _salesValidationService = salesValidationService;
        }

        public async Task<Result<Guid>> Handle(OpenCashierShiftCommand request, CancellationToken cancellationToken)
        {
            var tenantId = _currentUserService.TenantId;

            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException(
                    "A valid tenant is required.");

            if (!Guid.TryParse(_currentUserService.UserId, out var userId) || userId == Guid.Empty)
                throw new UnauthorizedAccessException("A valid user is required.");

            var validationResult = await _salesValidationService.ValidateTerminalAsync(request.BranchId, request.TerminalId, cancellationToken);

            if(validationResult.IsFailure)
                return Result<Guid>.Failure("Invalid terminal.");

            var hasOpenedShift = await _cashierShiftRepository.HasOpenedShiftAsync(tenantId.Value, userId, cancellationToken);

            if (hasOpenedShift)
                return Result<Guid>.Failure("This cashier already has an open shift.");

            var isTerminalInUse = await _cashierShiftRepository.IsTerminalInUseAsync(tenantId.Value, request.BranchId,request.TerminalId, cancellationToken);

            if (isTerminalInUse)
                return Result<Guid>.Failure("The selected terminal is already in use.");

            var cashierShift = new CashierShift
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                BranchId = request.BranchId,
                TerminalId = request.TerminalId,
                CashierUserId = userId,
                OpeningCash = request.OpeningCash,
                Status = CashierShiftStatus.Open,
                OpenedAt = DateTime.UtcNow
            };

            await _cashierShiftRepository.AddAsync(cashierShift, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(cashierShift.Id);
        }
    }
}
