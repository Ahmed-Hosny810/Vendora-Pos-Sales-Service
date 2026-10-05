
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pos.SalesService.Application.Interfaces.Publisher;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Infrastructure.Shared.Options;
using Pos.SalesService.Infrastructure.Shared.Publisher;
using Pos.SalesService.Infrastructure.Shared.Services;
using Pos.SalesService.Infrastructure.Shared.Workers;

namespace Pos.SalesService.Infrastructure.Shared
{
    public static class ServiceRegistrations
    {
        public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ISaleCalculationService, SaleCalculationService>();

            services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

            services.AddHostedService<OutboxPublisherWorker>();

            //services.AddScoped<OutboxDispatcher>();

            // Options Registerations
            services.Configure<RabbitMqOptions>(
                configuration.GetSection(nameof(RabbitMqOptions)));

            services.Configure<OutboxPublisherOptions>(
                configuration.GetSection(nameof(OutboxPublisherOptions)));

            return services;
        }
    }
}
