using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.SalesService.Application.Common.Constants;
using Pos.SalesService.Application.Features.SalePayments.Commands.RecordCommand;
using Pos.SalesService.Application.Features.SalePayments.Commands.ConfirmCommand;
using Pos.SalesService.Application.Features.SalePayments.Commands.CancelPendingCommand;
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
    public async Task<IActionResult> GetAll(Guid saleId,
        [FromQuery] GetSalePaymentsQueryParameter parameter, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSalePaymentsQuery { SaleId = saleId, Parameter = parameter }, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result);
    }

    [HttpPost]
    [Authorize(Policy = SalesPolicies.CompleteSales)]
    public async Task<IActionResult> Record(Guid saleId, [FromBody] RecordSalePaymentCommand command,
        CancellationToken cancellationToken)
    {
        if (saleId != command.SaleId) return BadRequest("Route ID must match SaleId.");
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result) : Conflict(result);
    }

    // This is cashier attestation after checking a separate terminal, not a gateway callback.
    [HttpPost("{paymentId:guid}/confirm")]
    [Authorize(Policy = SalesPolicies.CompleteSales)]
    public async Task<IActionResult> Confirm(Guid saleId, Guid paymentId,
        [FromBody] ConfirmSalePaymentCommand command, CancellationToken cancellationToken)
    {
        if (saleId != command.SaleId || paymentId != command.PaymentId)
            return BadRequest("Route IDs must match SaleId and PaymentId.");
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result) : Conflict(result);
    }

    [HttpPost("{paymentId:guid}/cancel")]
    [Authorize(Policy = SalesPolicies.CompleteSales)]
    public async Task<IActionResult> Cancel(Guid saleId, Guid paymentId,
        [FromBody] CancelPendingSalePaymentCommand command, CancellationToken cancellationToken)
    {
        if (saleId != command.SaleId || paymentId != command.PaymentId)
            return BadRequest("Route IDs must match SaleId and PaymentId.");
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result) : Conflict(result);
    }
}

