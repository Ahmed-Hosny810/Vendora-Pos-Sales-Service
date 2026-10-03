using AutoMapper;
using MediatR;
using Pos.SalesService.Application.Features.Sales.DTOs;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;

namespace Pos.SalesService.Application.Features.Sales.Queries.GetByIdQuery
{
    public class GetSaleByIdQuery : IRequest<Result<SaleDetailsDto>>
    {
        public Guid SaleId { get; set; }
    }

    public class GetSaleByIdQueryHandler : IRequestHandler<GetSaleByIdQuery, Result<SaleDetailsDto>>
    {
        private readonly ISaleRepositoryAsync _saleRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;

        public GetSaleByIdQueryHandler(ISaleRepositoryAsync saleRepository, ICurrentUserService currentUser, IMapper mapper)
        {
            _saleRepository = saleRepository;
            _currentUser = currentUser;
            _mapper = mapper;
        }

        public async Task<Result<SaleDetailsDto>> Handle(GetSaleByIdQuery request, CancellationToken cancellationToken)
        {
            var tenantId = _currentUser.TenantId;
            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException("A valid tenant is required.");

            var sale = await _saleRepository.GetByIdAsync(tenantId.Value, request.SaleId, cancellationToken);
            if (sale == null)
                return Result<SaleDetailsDto>.Failure("Sale was not found.");

            var result = _mapper.Map<SaleDetailsDto>(sale);
            result.Items = result.Items.OrderBy(i => i.ItemNumber).ThenBy(i => i.Id).ToList();
            return Result<SaleDetailsDto>.Success(result);
        }
    }
}