using Pos.SalesService.Application.Features.PaymentMethods.DTOs;
using Pos.SalesService.Application.Wrappers;
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
    public async Task<ActionResult<PagedResponse<IEnumerable<PaymentMethodDto>>>> GetAll([FromQuery] GetPaymentMethodsQuery query, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = SalesPolicies.ViewSales)]
    public async Task<ActionResult<Response<PaymentMethodDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPaymentMethodByIdQuery { PaymentMethodId = id }, cancellationToken);
        return result.IsSuccess ? Ok(new Response<PaymentMethodDto>(data: result.Value!)) : NotFound(new Response<PaymentMethodDto>(message: string.Join(", ", result.Errors)) { Errors = result.Errors.ToList() });
    }

    [HttpPost]
    [Authorize(Policy = SalesPolicies.ManagePaymentMethods)]
    public async Task<ActionResult<Response<Guid>>> Create([FromBody] CreatePaymentMethodCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, new Response<Guid>(data: result.Value!)) : BadRequest(new Response<Guid>(message: string.Join(", ", result.Errors)) { Errors = result.Errors.ToList() });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = SalesPolicies.ManagePaymentMethods)]
    public async Task<ActionResult<Response<Guid>>> Update(Guid id, [FromBody] UpdatePaymentMethodCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.PaymentMethodId)
            return BadRequest(new Response<Guid>(message: "Route ID must match PaymentMethodId."));
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(new Response<Guid>(data: result.Value!)) : BadRequest(new Response<Guid>(message: string.Join(", ", result.Errors)) { Errors = result.Errors.ToList() });
    }
}
