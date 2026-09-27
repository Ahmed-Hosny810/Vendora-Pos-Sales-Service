using Pos.SalesService.Application;
using Pos.SalesService.Infrastructure.Persistence;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.WebApi.Extensions;
using Pos.SalesService.WebApi.MiddleWares;
using Pos.SalesService.WebApi.Services;
using Serilog;
using Pos.SalesService.WebApi.Policies;

namespace Pos.SalesService.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext();
            });

            // API Versioning
            builder.Services.AddApiVersioningExtension();


            //-------------------Services Registration-----------------------

            builder.Services.AddPersistenceServices(builder.Configuration);

            //builder.Services.AddSharedInfrastructureServices(builder.Configuration);

            builder.Services.AddApplicationLayer(builder.Configuration);

            builder.Services.AddControllers();

            builder.Services.AddAuthenticationServices(builder.Configuration);

            builder.Services.AddAppPolicies();

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

            // Swagger (via extension)
            builder.Services.AddSwaggerExtension();

            var app = builder.Build();

            app.UseMiddleware<ErrorHandlerMiddleware>();


            if (app.Environment.IsDevelopment())
            {
                app.UseSwaggerExtension();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
