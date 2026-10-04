namespace Pos.SalesService.Application.Features.SalesReturns.DTOs;

public class ReturnableSaleDto
{
    public string Status { get; set; } = string.Empty;
    public List<ReturnableSaleItemDto> Items { get; set; } = new();
}
