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


        [HttpGet("{id:guid}")]
        [Authorize(Policy = SalesPolicies.ViewSales)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSaleByIdQuery { SaleId = id }, cancellationToken);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpPut("draft")]
        [Authorize(Policy = SalesPolicies.EditSales)]
        public async Task<IActionResult> UpdateDraft([FromBody] UpdateSaleDraftCommand command,CancellationToken cancellationToken)
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