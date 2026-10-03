
using AutoMapper;
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
            CreateMap<Customer, CustomerDto>();
            CreateMap<CashierShift, CashierShiftDto>();
            CreateMap<PaymentMethod, PaymentMethodDto>();
            CreateMap<Sale, SaleDetailsDto>();
            CreateMap<SaleItem, SaleItemDetailsDto>();
        }
    }
}
