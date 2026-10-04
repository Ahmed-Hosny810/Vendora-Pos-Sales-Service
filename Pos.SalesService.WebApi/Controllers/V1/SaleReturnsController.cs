using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.SalesService.Application.Common.Constants;
using Pos.SalesService.Application.Features.SalesReturns.Commands.CreateReturnDraftCommand;
using Pos.SalesService.Application.Features.SalesReturns.Commands.UpdateReturnDraftCommand;
using Pos.SalesService.Application.Features.SalesReturns.Commands.CancelReturnDraftCommand;
using Pos.SalesService.Application.Features.SalesReturns.DTOs;
using Pos.SalesService.Application.Features.SalesReturns.Queries.GetAllQuery;
using Pos.SalesService.Application.Features.SalesReturns.Queries.GetByIdQuery;
using Pos.SalesService.Application.Features.SalesReturns.Queries.GetReturnableItemsQuery;
using Pos.SalesService.Application.Wrappers;

namespace Pos.SalesService.WebApi.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class SaleReturnsController : ControllerBase
{
    private readonly IMediator _mediator;
    public SaleReturnsController(IMediator mediator) { _mediator = mediator; }

    [HttpPost]
    [Authorize(Policy = SalesPolicies.ManageReturns)]
    public async Task<ActionResult<Response<Guid>>> Create(
        [FromBody] CreateSaleReturnDraftCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new Response<Guid>(string.Join(", ", result.Errors)) { Errors = result.Errors });
        return StatusCode(StatusCodes.Status201Created, new Response<Guid>(result.Value));
    }

    [HttpPut("{id:guid}/draft")]
    [Authorize(Policy = SalesPolicies.ManageReturns)]
    public async Task<ActionResult<Response<Guid>>> Update(Guid id,
        [FromBody] UpdateSaleReturnDraftCommand command, CancellationToken cancellationToken)
    {
        if (id != command.ReturnId)
            return BadRequest(new Response<Guid>("Route ID must match ReturnId."));
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(new Response<Guid>(result.Value))
            : Conflict(new Response<Guid>(string.Join(", ", result.Errors)) { Errors = result.Errors });
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = SalesPolicies.ManageReturns)]
    public async Task<ActionResult<Response<Guid>>> Cancel(Guid id,
        [FromBody] CancelSaleReturnDraftCommand command, CancellationToken cancellationToken)
    {
        if (id != command.ReturnId)
            return BadRequest(new Response<Guid>("Route ID must match ReturnId."));
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(new Response<Guid>(result.Value))
            : Conflict(new Response<Guid>(string.Join(", ", result.Errors)) { Errors = result.Errors });
    }

    [HttpGet]
    [Authorize(Policy = SalesPolicies.ViewSales)]
    public async Task<ActionResult<PagedResponse<IEnumerable<SaleReturnSummaryDto>>>> GetAll(
        [FromQuery] GetSaleReturnsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value)
            : BadRequest(new Response<IEnumerable<SaleReturnSummaryDto>>(string.Join(", ", result.Errors)) { Errors = result.Errors });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = SalesPolicies.ViewSales)]
    public async Task<ActionResult<Response<SaleReturnDetailsDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSaleReturnByIdQuery { ReturnId = id }, cancellationToken);
        return result.IsSuccess ? Ok(new Response<SaleReturnDetailsDto>(result.Value!))
            : NotFound(new Response<SaleReturnDetailsDto>(string.Join(", ", result.Errors)) { Errors = result.Errors });
    }

    [HttpGet("returnable-items/{saleId:guid}")]
    [Authorize(Policy = SalesPolicies.ManageReturns)]
    public async Task<ActionResult<Response<IEnumerable<ReturnableSaleItemDto>>>> GetReturnableItems(
        Guid saleId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReturnableSaleItemsQuery { SaleId = saleId }, cancellationToken);
        return result.IsSuccess ? Ok(new Response<IEnumerable<ReturnableSaleItemDto>>(result.Value!))
            : BadRequest(new Response<IEnumerable<ReturnableSaleItemDto>>(string.Join(", ", result.Errors)) { Errors = result.Errors });
    }
}

