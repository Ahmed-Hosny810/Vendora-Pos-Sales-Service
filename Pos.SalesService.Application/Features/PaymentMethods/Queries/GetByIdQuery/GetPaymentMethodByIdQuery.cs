using MediatR;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using AutoMapper;
using Pos.SalesService.Application.Features.PaymentMethods.DTOs;
namespace Pos.SalesService.Application.Features.PaymentMethods.Queries.GetByIdQuery;
public class GetPaymentMethodByIdQuery : IRequest<Result<PaymentMethodDto>>
{
    public Guid PaymentMethodId { get; set; }
}
public class GetPaymentMethodByIdQueryHandler : IRequestHandler<GetPaymentMethodByIdQuery, Result<PaymentMethodDto>>
{
    private readonly IPaymentMethodRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    public GetPaymentMethodByIdQueryHandler(IPaymentMethodRepositoryAsync repository,
        ICurrentUserService currentUserService, IMapper mapper)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }
    public async Task<Result<PaymentMethodDto>> Handle(GetPaymentMethodByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            throw new UnauthorizedAccessException("A valid tenant is required.");

        var method = await _repository.GetPaymentMethodByIdAsync(
            tenantId.Value, request.PaymentMethodId, cancellationToken);

        return method == null ? Result<PaymentMethodDto>.Failure("Payment method was not found.")
            : Result<PaymentMethodDto>.Success(_mapper.Map<PaymentMethodDto>(method));
    }
}
