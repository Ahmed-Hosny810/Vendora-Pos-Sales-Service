using Asp.Versioning;
using Pos.SalesService.Application.Features.RefundPayments.DTOs;
using Pos.SalesService.Application.Features.RefundPayments.Queries.GetAllQuery;
using Pos.SalesService.Application.Features.SalesReturns.DTOs.Receipts;
using Pos.SalesService.Application.Features.SalesReturns.Queries.GetReceiptQuery;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.SalesService.Application.Common.Constants;
using Pos.SalesService.Application.Features.RefundPayments.Commands.RecordRefundCommand;
using Pos.SalesService.Application.Features.SalesReturns.Commands.CompleteReturnCommand;
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

    [HttpGet("{id:guid}/refund-payments")]
    [Authorize(Policy = SalesPolicies.ViewSales)]
    public async Task<ActionResult<PagedResponse<IEnumerable<RefundPaymentDto>>>> GetRefundPayments(
        Guid id, [FromQuery] GetRefundPaymentsQueryParameter parameter, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRefundPaymentsQuery
        {
            ReturnId = id,
            Parameter = parameter
        }, cancellationToken);
        return result.IsSuccess ? Ok(result.Value)
            : NotFound(new Response<IEnumerable<RefundPaymentDto>>(string.Join(", ", result.Errors))
            {
                Errors = result.Errors
            });
    }

    [HttpGet("{id:guid}/receipt")]
    [Authorize(Policy = SalesPolicies.ViewReceipts)]
    public async Task<ActionResult<Response<SaleReturnReceiptDto>>> GetReceipt(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSaleReturnReceiptQuery { ReturnId = id }, cancellationToken);
        return result.IsSuccess ? Ok(new Response<SaleReturnReceiptDto>(result.Value!))
            : NotFound(new Response<SaleReturnReceiptDto>(string.Join(", ", result.Errors))
            {
                Errors = result.Errors
            });
    }

    [HttpPost]
    [Authorize(Policy = SalesPolicies.ManageReturns)]
    public async Task<ActionResult<Response<Guid>>> Complete(
        [FromBody] CompleteSaleReturnCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new Response<Guid>(string.Join(", ", result.Errors)) { Errors = result.Errors });
        return StatusCode(StatusCodes.Status201Created, new Response<Guid>(result.Value));
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


    [HttpPost("refund-payments")]
    [Authorize(Policy = SalesPolicies.ProcessRefunds)]
    public async Task<ActionResult<Response<Guid>>> RecordRefundPayment([FromBody] RecordRefundPaymentCommand command,CancellationToken cancellationToken)
    {
      
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Conflict(new Response<Guid>(
                message: string.Join(", ", result.Errors))
            {
                Errors = result.Errors
            });
        }

        return Ok(new Response<Guid>(
            data: result.Value,
            message: "Refund payment recorded successfully."));
    }
}

