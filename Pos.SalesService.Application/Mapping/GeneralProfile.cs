
using AutoMapper;
using Pos.SalesService.Application.Features.PaymentMethods.DTOs;
using Pos.SalesService.Application.Features.Customers.DTOs;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Mapping
{
    public class GeneralProfile:Profile
    {
        public GeneralProfile()
        {
            CreateMap<Customer, CustomerDto>();
            CreateMap<PaymentMethod, PaymentMethodDto>();
            
        }
    }
}
