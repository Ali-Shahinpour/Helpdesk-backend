using AutoMapper;
using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Common.Interfaces;
using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using INotificationPublisher = HelpDesk.Application.Common.Interfaces.INotificationPublisher;

namespace HelpDesk.Application.Features.Comments;

public record ListCommentsQuery(Guid TicketId, bool IncludeInternal) : IRequest<IReadOnlyList<CommentDto>>;
public record AddCommentCommand(Guid TicketId, string Body, bool IsInternal) : IRequest<CommentDto>;

public class ListCommentsHandler(IUnitOfWork uow, IMapper map) : IRequestHandler<ListCommentsQuery, IReadOnlyList<CommentDto>>
{
    public async Task<IReadOnlyList<CommentDto>> Handle(ListCommentsQuery q, CancellationToken ct)
    {
        var query = uow.Repo<Comment>().Query().Where(c => c.TicketId == q.TicketId);
        if (!q.IncludeInternal) query = query.Where(c => !c.IsInternal);
        var list = await query.OrderBy(c => c.CreatedAt).ToListAsync(ct);
        return map.Map<List<CommentDto>>(list);
    }
}

public class AddCommentHandler(IUnitOfWork uow, ICurrentUser current, INotificationPublisher pub, IMapper map)
    : IRequestHandler<AddCommentCommand, CommentDto>
{
    public async Task<CommentDto> Handle(AddCommentCommand c, CancellationToken ct)
    {
        var actor = current.Id ?? throw new UnauthorizedAccessException();
        var ticket = await uow.Tickets.GetByIdAsync(c.TicketId, ct) ?? throw new KeyNotFoundException();
        var comment = new Comment { TicketId = c.TicketId, AuthorId = actor, Body = c.Body, IsInternal = c.IsInternal };
        await uow.Repo<Comment>().AddAsync(comment, ct);
        ticket.Activities.Add(new ActivityEvent { TicketId = c.TicketId, ActorId = actor, Type = ActivityType.Commented });
        ticket.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
        await pub.CommentAddedAsync(comment, ct);
        return map.Map<CommentDto>(comment);
    }
}
