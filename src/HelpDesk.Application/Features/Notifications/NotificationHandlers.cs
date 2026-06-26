using AutoMapper;
using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Common.Interfaces;
using HelpDesk.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.Features.Notifications;

public record ListNotificationsQuery(bool UnreadOnly) : IRequest<IReadOnlyList<NotificationDto>>;
public record MarkNotificationReadCommand(Guid Id) : IRequest<Unit>;
public record MarkAllReadCommand() : IRequest<Unit>;

public class ListNotificationsHandler(IUnitOfWork uow, ICurrentUser current, IMapper map) : IRequestHandler<ListNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public async Task<IReadOnlyList<NotificationDto>> Handle(ListNotificationsQuery q, CancellationToken ct)
    {
        var userId = current.Id ?? throw new UnauthorizedAccessException();
        var query = uow.Repo<Notification>().Query().Where(n => n.UserId == userId);
        if (q.UnreadOnly) query = query.Where(n => !n.IsRead);
        var list = await query.OrderByDescending(n => n.CreatedAt).Take(100).ToListAsync(ct);
        return map.Map<List<NotificationDto>>(list);
    }
}

public class MarkNotificationReadHandler(IUnitOfWork uow, ICurrentUser current) : IRequestHandler<MarkNotificationReadCommand, Unit>
{
    public async Task<Unit> Handle(MarkNotificationReadCommand c, CancellationToken ct)
    {
        var userId = current.Id ?? throw new UnauthorizedAccessException();
        var n = await uow.Repo<Notification>().GetByIdAsync(c.Id, ct);
        if (n is null || n.UserId != userId) return Unit.Value;
        n.IsRead = true;
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public class MarkAllReadHandler(IUnitOfWork uow, ICurrentUser current) : IRequestHandler<MarkAllReadCommand, Unit>
{
    public async Task<Unit> Handle(MarkAllReadCommand _, CancellationToken ct)
    {
        var userId = current.Id ?? throw new UnauthorizedAccessException();
        var all = await uow.Repo<Notification>().Query().Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
        foreach (var n in all) n.IsRead = true;
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
