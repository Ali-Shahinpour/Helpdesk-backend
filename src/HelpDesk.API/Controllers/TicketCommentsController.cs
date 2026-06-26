using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Common.Interfaces;
using HelpDesk.Application.Features.Comments;
using HelpDesk.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.API.Controllers;

[ApiController, Route("api/tickets/{ticketId:guid}/comments"), Authorize]
public class TicketCommentsController(IMediator m, ICurrentUser current) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CommentDto>> List(Guid ticketId)
    {
        var canSeeInternal = current.Role is UserRole.Admin or UserRole.Manager or UserRole.Agent;
        return m.Send(new ListCommentsQuery(ticketId, canSeeInternal));
    }

    [HttpPost]
    public Task<CommentDto> Add(Guid ticketId, [FromBody] AddCommentRequest r)
    {
        // Customers can't write internal notes.
        var isInternal = r.IsInternal && current.Role is UserRole.Admin or UserRole.Manager or UserRole.Agent;
        return m.Send(new AddCommentCommand(ticketId, r.Body, isInternal));
    }
}
