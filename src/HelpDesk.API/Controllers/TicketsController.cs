using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Features.Tickets;
using HelpDesk.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.API.Controllers;

[ApiController, Route("api/tickets"), Authorize]
public class TicketsController(IMediator m) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<TicketDto>> List([FromQuery] TicketStatus? status, [FromQuery] Guid? assignedAgentId, [FromQuery] Guid? customerId, [FromQuery] string? q)
        => m.Send(new ListTicketsQuery(status, assignedAgentId, customerId, q));

    [HttpGet("{id:guid}")]
    public Task<TicketDto> Get(Guid id) => m.Send(new GetTicketQuery(id));

    [HttpPost]
    public Task<TicketDto> Create([FromBody] CreateTicketRequest r)
        => m.Send(new CreateTicketCommand(r.Subject, r.Description, r.Priority, r.Category, r.DepartmentId));

    [HttpPut("{id:guid}"), Authorize(Roles = "Admin,Manager,Agent")]
    public Task<TicketDto> Update(Guid id, [FromBody] UpdateTicketRequest r) => m.Send(new UpdateTicketCommand(id, r));

    [HttpDelete("{id:guid}"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(Guid id) { await m.Send(new DeleteTicketCommand(id)); return NoContent(); }
}
