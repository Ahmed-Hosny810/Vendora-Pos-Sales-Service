using Microsoft.AspNetCore.Authorization;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Pos.SalesService.Application.Common.Constants;
using System.Security.Claims;

namespace Pos.SalesService.WebApi.Policies
{
    public static class AppPolicies
    {
        public static IServiceCollection AddAppPolicies(
            this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                var defaultPolicy = new AuthorizationPolicyBuilder();
                RequireTenant(defaultPolicy);
                defaultPolicy.RequireRole(
                    SalesRoles.TenantOwner,
                    SalesRoles.Admin,
                    SalesRoles.Cashier);

                options.DefaultPolicy = defaultPolicy.Build();
                options.FallbackPolicy = options.DefaultPolicy;

                AddRolePolicy(options, SalesPolicies.ViewSales,
                    SalesRoles.TenantOwner,
                    SalesRoles.Admin,
                    SalesRoles.Cashier);

                AddRolePolicy(options, SalesPolicies.ManageCustomers,
                    SalesRoles.TenantOwner,
                    SalesRoles.Admin,
                    SalesRoles.Cashier);

                AddRolePolicy(options, SalesPolicies.ManagePaymentMethods,
                    SalesRoles.TenantOwner,
                    SalesRoles.Admin);

                AddRolePolicy(options, SalesPolicies.OperateShifts,
                    SalesRoles.TenantOwner,
                    SalesRoles.Admin,
                    SalesRoles.Cashier);

                AddRolePolicy(options, SalesPolicies.EditSales,
                    SalesRoles.TenantOwner,
                    SalesRoles.Admin,
                    SalesRoles.Cashier);

                AddRolePolicy(options, SalesPolicies.ApplyDiscounts,
                    SalesRoles.TenantOwner,
                    SalesRoles.Admin,
                    SalesRoles.Cashier);

                AddRolePolicy(options, SalesPolicies.ApproveDiscounts,
                    SalesRoles.TenantOwner,
                    SalesRoles.Admin);

                AddRolePolicy(options, SalesPolicies.CompleteSales,
                    SalesRoles.TenantOwner,
                    SalesRoles.Admin,
                    SalesRoles.Cashier);

                AddRolePolicy(options, SalesPolicies.CancelSales,
                    SalesRoles.TenantOwner,
                    SalesRoles.Admin,
                    SalesRoles.Cashier);

                AddRolePolicy(options, SalesPolicies.ManageReturns,
                    SalesRoles.TenantOwner,
                    SalesRoles.Admin,
                    SalesRoles.Cashier);

                AddRolePolicy(options, SalesPolicies.ApproveReturns,
                    SalesRoles.TenantOwner,
                    SalesRoles.Admin);

                AddRolePolicy(options, SalesPolicies.ProcessRefunds,
                    SalesRoles.TenantOwner,
                    SalesRoles.Admin);

                AddRolePolicy(options, SalesPolicies.ViewReceipts,
                    SalesRoles.TenantOwner,
                    SalesRoles.Admin,
                    SalesRoles.Cashier);

            });

            return services;
        }

        private static void RequireTenant(AuthorizationPolicyBuilder policy)
        {
            policy.AuthenticationSchemes.Add(
                OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

            policy.RequireAuthenticatedUser();

            policy.RequireClaim("user_type", "Tenant");

            policy.RequireAssertion(context =>
                Guid.TryParse(
                    context.User.FindFirstValue("tenant_id"),
                    out var tenantId)
                && tenantId != Guid.Empty
                && Guid.TryParse(
                    context.User.FindFirstValue(OpenIddictConstants.Claims.Subject)
                        ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    out var userId)
                && userId != Guid.Empty);
        }

        private static void AddRolePolicy(
            AuthorizationOptions options,
            string policyName,
            params string[] roles)
        {
            options.AddPolicy(policyName, policy =>
            {
                RequireTenant(policy);
                policy.RequireRole(roles);
            });
        }
    }
}
