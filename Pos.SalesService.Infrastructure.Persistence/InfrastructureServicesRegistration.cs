using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Infrastructure.Persistence.Contexts;
using Pos.SalesService.Infrastructure.Persistence.Repositories;
using Pos.SalesService.Infrastructure.Persistence.UnitofWork;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Infrastructure.Persistence.Services;

namespace Pos.SalesService.Infrastructure.Persistence
{
    public static class InfrastructureServicesRegistration
    {
        public static IServiceCollection AddPersistenceServices(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(
                        configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions => {
                        sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "sales");
                    }));

            services.AddScoped(typeof(IGenericRepositoryAsync<,>), typeof(GenericRepositoryAsync<,>));

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<ICustomerRepositoryAsync, CustomerRepositoryAsync>();
            services.AddScoped<ISaleRepositoryAsync, SaleRepositoryAsync>();
            services.AddScoped<ISalePaymentRepositoryAsync, SalePaymentRepositoryAsync>();
            services.AddScoped<Pos.SalesService.Application.Features.SalePayments.Services.SalePaymentWorkflow>();
            services.AddScoped<ICashierShiftRepositoryAsync, CashierShiftRepositoryAsync>();
            services.AddScoped<IPaymentMethodRepositoryAsync, PaymentMethodRepositoryAsync>();

            services.AddScoped<ISalesValidationService, SalesValidationService>();

            return services;
        }
    }
}
