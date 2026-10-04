
using AutoMapper;
using Pos.SalesService.Application.Features.SalesReturns.DTOs;
using Pos.SalesService.Application.Features.CashierShifts.DTOs;
using Pos.SalesService.Application.Features.Customers.DTOs;
using Pos.SalesService.Application.Features.PaymentMethods.DTOs;
using Pos.SalesService.Application.Features.Sales.DTOs;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Mapping
{
    public class GeneralProfile:Profile
    {
        public GeneralProfile()
        {
            CreateMap<SalePayment, Pos.SalesService.Application.Features.SalePayments.DTOs.SalePaymentDto>();
            CreateMap<Customer, CustomerDto>();
            CreateMap<CashierShift, CashierShiftDto>();
            CreateMap<PaymentMethod, PaymentMethodDto>();
            CreateMap<Sale, SaleDetailsDto>();
            CreateMap<SaleReturn, SaleReturnSummaryDto>();
            CreateMap<SaleReturn, SaleReturnDetailsDto>()
                .ForMember(x => x.Items, o => o.MapFrom(x => x.Items
                    .OrderBy(i => i.OriginalSaleItem.ItemNumber)
                    .ThenBy(i => i.StockCondition).ThenBy(i => i.Restock).ThenBy(i => i.Id)));
            CreateMap<SaleReturnItem, SaleReturnItemDto>()
                .ForMember(x => x.ProductNameSnapshot, o => o.MapFrom(x => x.OriginalSaleItem.ProductNameSnapshot))
                .ForMember(x => x.VariantNameSnapshot, o => o.MapFrom(x => x.OriginalSaleItem.VariantNameSnapshot))
                .ForMember(x => x.UnitNameSnapshot, o => o.MapFrom(x => x.OriginalSaleItem.UnitNameSnapshot));
            CreateMap<Sale, SaleSummaryDto>();
            CreateMap<SaleStatusHistory, SaleStatusHistoryDto>();
            CreateMap<SaleItem, SaleItemDetailsDto>();
        }
    }
}
