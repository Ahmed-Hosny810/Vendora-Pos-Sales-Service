using Pos.SalesService.Application.Parameters;

namespace Pos.SalesService.Application.Features.Customers.Queries.GetCustomersQuery;

public class GetCustomersQueryParameter : RequestParameter<CustomerOrderKey>
{
    public CustomerFilter? Filter { get; set; }
}

public class CustomerFilter
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}

public enum CustomerOrderKey
{
    CreatedAt,
    FullName,
    Phone,
    UpdatedAt
}
