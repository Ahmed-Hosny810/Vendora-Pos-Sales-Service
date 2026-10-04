using Pos.SalesService.Application.Features.SalePayments.DTOs;
using Pos.SalesService.Application.Wrappers;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.SalesService.Application.Common.Constants;
using Pos.SalesService.Application.Features.SalePayments.Commands.RecordCommand;
using Pos.SalesService.Application.Features.SalePayments.Queries.GetAllQuery;

namespace Pos.SalesService.WebApi.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/Sales/{saleId:guid}/payments")]
public class SalePaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public SalePaymentsController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    [Authorize(Policy = SalesPolicies.ViewSales)]
    public async Task<ActionResult<PagedResponse<IEnumerable<SalePaymentDto>>>> GetAll(Guid saleId,
        [FromQuery] GetSalePaymentsQueryParameter parameter, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSalePaymentsQuery { SaleId = saleId, Parameter = parameter }, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new Response<IEnumerable<SalePaymentDto>>(message: string.Join(", ", result.Errors)) { Errors = result.Errors.ToList() });
    }

    [HttpPost]
    [Authorize(Policy = SalesPolicies.CompleteSales)]
    public async Task<ActionResult<Response<Guid>>> Record(Guid saleId, [FromBody] RecordSalePaymentCommand command,
        CancellationToken cancellationToken)
    {
        if (saleId != command.SaleId) return BadRequest(new Response<Guid>(message: "Route ID must match SaleId."));
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(new Response<Guid>(data: result.Value!)) : Conflict(new Response<Guid>(message: string.Join(", ", result.Errors)) { Errors = result.Errors.ToList() });
    }

}
