using MediatR;
using Pos.SalesService.Application.DTOS.ReadModelsDTOs;
using Pos.SalesService.Application.Features.Sales.DTOs.Receipts;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;

namespace Pos.SalesService.Application.Features.Sales.Queries.GetReceiptQuery
{
    public class GetSaleReceiptQuery:IRequest<Result<SaleReceiptDto>>
    {
        public Guid SaleId { get; set; }
    }
    public class GetSaleReceiptQueryHandler : IRequestHandler<GetSaleReceiptQuery, Result<SaleReceiptDto>>
    {
        private readonly ISaleRepositoryAsync _saleRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IReadModelsRepositoryAsync _readModelsRepository;

        public GetSaleReceiptQueryHandler(
            ISaleRepositoryAsync saleRepository,
            ICurrentUserService currentUser,
            IReadModelsRepositoryAsync readModelsRepository)
        {
            _saleRepository = saleRepository;
            _currentUser = currentUser;
            _readModelsRepository = readModelsRepository;
        }

        public async Task<Result<SaleReceiptDto>> Handle(GetSaleReceiptQuery request, CancellationToken cancellationToken)
        {
            if (!_currentUser.TenantId.HasValue || _currentUser.TenantId == Guid.Empty)
                return Result<SaleReceiptDto>.Failure("A valid tenant is required.");

            var tenantId = _currentUser.TenantId.Value;

            var receipt = await _saleRepository.GetSaleReceiptAsync(tenantId, request.SaleId, cancellationToken);

            if (receipt == null)
                return Result<SaleReceiptDto>.Failure("Receipt not found. The sale may not exist or is not completed.");

            BranchReadModelDto? branchInfo = await _readModelsRepository.GetReceiptInfoAsync(tenantId, receipt.BranchId, cancellationToken);

            receipt.BranchNameAr = branchInfo?.NameAr;
            receipt.BranchNameEn = branchInfo?.NameEn ?? string.Empty;
            receipt.ReceiptHeader = branchInfo?.ReceiptHeader;

          //  receipt.ReceiptFooter = await _tenantSettingsReadModelRepository.GetReceiptFooterAsync(tenantId, cancellationToken);

            return Result<SaleReceiptDto>.Success(receipt);
        }
    }
}
