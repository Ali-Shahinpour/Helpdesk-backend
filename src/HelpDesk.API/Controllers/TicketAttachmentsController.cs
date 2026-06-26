using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Features.Attachments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.API.Controllers;

[ApiController, Authorize]
public class TicketAttachmentsController(IMediator m) : ControllerBase
{
    [HttpGet("api/tickets/{ticketId:guid}/attachments")]
    public Task<IReadOnlyList<AttachmentDto>> List(Guid ticketId) => m.Send(new ListAttachmentsQuery(ticketId));

    [HttpPost("api/tickets/{ticketId:guid}/attachments"), RequestSizeLimit(25_000_000)]
    public async Task<AttachmentDto> Upload(Guid ticketId, IFormFile file)
    {
        if (file is null || file.Length == 0) throw new ArgumentException("File required");
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        return await m.Send(new UploadAttachmentCommand(ticketId, file.FileName, file.Length,
            file.ContentType ?? "application/octet-stream", ms.ToArray()));
    }

    [HttpGet("api/attachments/{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var r = await m.Send(new GetAttachmentContentQuery(id));
        if (r is null) return NotFound();
        return File(r.Value.Content, r.Value.ContentType, r.Value.FileName);
    }

    [HttpDelete("api/attachments/{id:guid}"), Authorize(Roles = "Admin,Manager,Agent")]
    public async Task<IActionResult> Delete(Guid id) { await m.Send(new DeleteAttachmentCommand(id)); return NoContent(); }
}
