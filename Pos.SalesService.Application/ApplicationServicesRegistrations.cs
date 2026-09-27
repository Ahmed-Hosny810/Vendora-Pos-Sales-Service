using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pos.SalesService.Application.Behaviours;
using System.Reflection;

namespace Pos.SalesService.Application
{
    public static class ApplicationServicesRegistrations
    {
        public static void AddApplicationLayer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAutoMapper(cfg =>
                cfg.AddMaps(Assembly.GetExecutingAssembly())
            );
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        }
    }
}
