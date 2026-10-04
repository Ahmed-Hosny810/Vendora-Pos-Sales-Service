using Pos.SalesService.Application.Features.Sales.DTOs.Receipts;
using Pos.SalesService.Application.Features.Sales.DTOs;
using Pos.SalesService.Application.Features.Sales.Queries.GetSalesQuery;
using Pos.SalesService.Application.Features.Sales.Queries.GetStatusHistoryQuery;
using Pos.SalesService.Application.Features.Sales.Queries.GetReceiptQuery;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.SalesService.Application.Common.Constants;
using Pos.SalesService.Application.Features.Sales.Commands.CreateDraftCommand;
using Pos.SalesService.Application.Features.Sales.Commands.UpdateDraftCommand;
using Pos.SalesService.Application.Features.Sales.Queries.GetByIdQuery;
using Pos.SalesService.Application.Wrappers;

namespace Pos.SalesService.WebApi.Controllers.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public SalesController(IMediator mediator) { _mediator = mediator; }


        [HttpPost("{id:guid}")]
        [Authorize(Policy = SalesPolicies.CompleteSales)]
        public async Task<ActionResult<Response<Guid>>> Create(CreateSaleDraftCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
                return BadRequest(new Response<Guid>(
                    message: string.Join(", ", result.Errors)));

            return Ok(new Response<Guid>(data: result.Value , message: "Sale draft created successfully."));
        }


        [HttpGet]
        [Authorize(Policy = SalesPolicies.ViewSales)]
        public async Task<ActionResult<PagedResponse<IEnumerable<SaleSummaryDto>>>> GetSales([FromQuery] GetSalesQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new Response<IEnumerable<SaleSummaryDto>>(message: string.Join(", ", result.Errors)) { Errors = result.Errors.ToList() });
        }

        [HttpGet("{id:guid}/status-history")]
        [Authorize(Policy = SalesPolicies.ViewSales)]
        public async Task<ActionResult<PagedResponse<IEnumerable<SaleStatusHistoryDto>>>> GetStatusHistory(Guid id,
            [FromQuery] GetSaleStatusHistoryQueryParameter parameter, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSaleStatusHistoryQuery
            {
                SaleId = id,
                Parameter = parameter
            }, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : NotFound(new Response<IEnumerable<SaleStatusHistoryDto>>(message: string.Join(", ", result.Errors)) { Errors = result.Errors.ToList() });
        }

        [HttpGet("{id:guid}/receipt")]
        [Authorize(Policy = SalesPolicies.ViewReceipts)]
        public async Task<ActionResult<Response<SaleReceiptDto>>> GetReceipt(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSaleReceiptQuery { SaleId = id }, cancellationToken);
            return result.IsSuccess ? Ok(new Response<SaleReceiptDto>(data: result.Value!)) : NotFound(new Response<SaleReceiptDto>(message: string.Join(", ", result.Errors)) { Errors = result.Errors.ToList() });
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = SalesPolicies.ViewSales)]
        public async Task<ActionResult<Response<SaleDetailsDto>>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSaleByIdQuery { SaleId = id }, cancellationToken);
            return result.IsSuccess ? Ok(new Response<SaleDetailsDto>(data: result.Value!)) : NotFound(new Response<SaleDetailsDto>(message: string.Join(", ", result.Errors)) { Errors = result.Errors.ToList() });
        }

        [HttpPut("draft")]
        [Authorize(Policy = SalesPolicies.EditSales)]
        public async Task<ActionResult<Response<Guid>>> UpdateDraft([FromBody] UpdateSaleDraftCommand command,CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
                return BadRequest(new Response<Guid>(
                    message: string.Join(", ", result.Errors)));

            return Ok(new Response<Guid>(
                data: result.Value,
                message: "Sale draft updated successfully."));
        }
    }
}