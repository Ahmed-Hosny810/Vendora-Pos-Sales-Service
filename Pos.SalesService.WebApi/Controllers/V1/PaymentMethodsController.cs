using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.SalesService.Application.Common.Constants;
using Pos.SalesService.Application.Features.PaymentMethods.Commands.CreateCommand;
using Pos.SalesService.Application.Features.PaymentMethods.Commands.UpdateCommand;
using Pos.SalesService.Application.Features.PaymentMethods.Queries.GetAllQuery;
using Pos.SalesService.Application.Features.PaymentMethods.Queries.GetByIdQuery;

namespace Pos.SalesService.WebApi.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PaymentMethodsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PaymentMethodsController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    [Authorize(Policy = SalesPolicies.ViewSales)]
    public async Task<IActionResult> GetAll([FromQuery] GetPaymentMethodsQuery query, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = SalesPolicies.ViewSales)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPaymentMethodByIdQuery { PaymentMethodId = id }, cancellationToken);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [Authorize(Policy = SalesPolicies.ManagePaymentMethods)]
    public async Task<IActionResult> Create([FromBody] CreatePaymentMethodCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = SalesPolicies.ManagePaymentMethods)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePaymentMethodCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.PaymentMethodId)
            return BadRequest("Route ID must match PaymentMethodId.");
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
