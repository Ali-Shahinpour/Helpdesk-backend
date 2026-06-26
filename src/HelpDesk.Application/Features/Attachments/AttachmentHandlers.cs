using AutoMapper;
using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Common.Interfaces;
using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.Features.Attachments;

public record ListAttachmentsQuery(Guid TicketId) : IRequest<IReadOnlyList<AttachmentDto>>;
public record UploadAttachmentCommand(Guid TicketId, string FileName, long Size, string ContentType, byte[] Content) : IRequest<AttachmentDto>;
public record DeleteAttachmentCommand(Guid Id) : IRequest<Unit>;
public record GetAttachmentContentQuery(Guid Id) : IRequest<(byte[] Content, string ContentType, string FileName)?>;

public class ListAttachmentsHandler(IUnitOfWork uow, IMapper map) : IRequestHandler<ListAttachmentsQuery, IReadOnlyList<AttachmentDto>>
{
    public async Task<IReadOnlyList<AttachmentDto>> Handle(ListAttachmentsQuery q, CancellationToken ct)
    {
        var list = await uow.Repo<Attachment>().Query().Where(a => a.TicketId == q.TicketId).OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
        return map.Map<List<AttachmentDto>>(list);
    }
}

public class UploadAttachmentHandler(IUnitOfWork uow, ICurrentUser current, IMapper map, IFileStorage storage)
    : IRequestHandler<UploadAttachmentCommand, AttachmentDto>
{
    public async Task<AttachmentDto> Handle(UploadAttachmentCommand c, CancellationToken ct)
    {
        var actor = current.Id ?? throw new UnauthorizedAccessException();
        var path = await storage.SaveAsync(c.TicketId, c.FileName, c.Content, ct);
        var a = new Attachment {
            TicketId = c.TicketId, FileName = c.FileName, Size = c.Size,
            ContentType = c.ContentType, StoragePath = path, UploadedById = actor,
        };
        await uow.Repo<Attachment>().AddAsync(a, ct);
        var ticket = await uow.Tickets.GetByIdAsync(c.TicketId, ct);
        ticket?.Activities.Add(new ActivityEvent { TicketId = c.TicketId, ActorId = actor, Type = ActivityType.AttachmentAdded, MetaJson = $"{{\"fileName\":\"{c.FileName}\"}}" });
        await uow.SaveChangesAsync(ct);
        return map.Map<AttachmentDto>(a);
    }
}

public class DeleteAttachmentHandler(IUnitOfWork uow, IFileStorage storage) : IRequestHandler<DeleteAttachmentCommand, Unit>
{
    public async Task<Unit> Handle(DeleteAttachmentCommand c, CancellationToken ct)
    {
        var a = await uow.Repo<Attachment>().GetByIdAsync(c.Id, ct) ?? throw new KeyNotFoundException();
        await storage.DeleteAsync(a.StoragePath, ct);
        uow.Repo<Attachment>().Remove(a);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public class GetAttachmentContentHandler(IUnitOfWork uow, IFileStorage storage) : IRequestHandler<GetAttachmentContentQuery, (byte[], string, string)?>
{
    public async Task<(byte[], string, string)?> Handle(GetAttachmentContentQuery q, CancellationToken ct)
    {
        var a = await uow.Repo<Attachment>().GetByIdAsync(q.Id, ct);
        if (a is null) return null;
        var bytes = await storage.ReadAsync(a.StoragePath, ct);
        return (bytes, a.ContentType, a.FileName);
    }
}
