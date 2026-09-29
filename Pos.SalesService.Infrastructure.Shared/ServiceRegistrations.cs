
using Microsoft.Extensions.DependencyInjection;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Infrastructure.Shared.Services;

namespace Pos.SalesService.Infrastructure.Shared
{
    public static class ServiceRegistrations
    {
        public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<ISaleCalculationService, SaleCalculationService>();

            return services;
        }
    }
}
