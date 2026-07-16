using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Features.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.API.Controllers;

[ApiController, Route("api/notifications"), Authorize]
public class NotificationsController(IMediator m) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<NotificationDto>> List([FromQuery] bool unreadOnly = false)
        => m.Send(new ListNotificationsQuery(unreadOnly));

    [HttpGet("unread-count")]
    public Task<int> UnreadCount() => m.Send(new GetUnreadNotificationCountQuery());

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id) { await m.Send(new MarkNotificationReadCommand(id)); return NoContent(); }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead() { await m.Send(new MarkAllReadCommand()); return NoContent(); }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) { await m.Send(new DeleteNotificationCommand(id)); return NoContent(); }

    [HttpDelete("read")]
    public async Task<IActionResult> DeleteAllRead() { await m.Send(new DeleteAllReadNotificationsCommand()); return NoContent(); }
}

