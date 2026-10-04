using Pos.SalesService.Application.Features.CashierShifts.DTOs;
using Pos.SalesService.Application.Wrappers;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.SalesService.Application.Common.Constants;
using Pos.SalesService.Application.Features.CashierShifts.Queries.GetAllQuery;
using Pos.SalesService.Application.Features.CashierShifts.Queries.GetByIdQuery;
using Pos.SalesService.Application.Features.CashierShifts.Queries.GetCurrentQuery;

namespace Pos.SalesService.WebApi.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = SalesPolicies.OperateShifts)]
public class CashierShiftsController : ControllerBase
{
    private readonly IMediator _mediator;
    public CashierShiftsController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<IEnumerable<CashierShiftDto>>>> GetAll([FromQuery] GetCashierShiftsQuery query,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(query, cancellationToken));

    [HttpGet("current")]
    public async Task<ActionResult<Response<CashierShiftDto>>> GetCurrent(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCurrentCashierShiftQuery(), cancellationToken);
        return result.IsSuccess ? Ok(new Response<CashierShiftDto>(data: result.Value!)) : NotFound(new Response<CashierShiftDto>(message: string.Join(", ", result.Errors)) { Errors = result.Errors.ToList() });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Response<CashierShiftDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCashierShiftByIdQuery { ShiftId = id }, cancellationToken);
        return result.IsSuccess ? Ok(new Response<CashierShiftDto>(data: result.Value!)) : NotFound(new Response<CashierShiftDto>(message: string.Join(", ", result.Errors)) { Errors = result.Errors.ToList() });
    }
}
