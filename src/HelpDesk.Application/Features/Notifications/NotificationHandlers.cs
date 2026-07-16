using AutoMapper;
using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Common.Interfaces;
using HelpDesk.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.Features.Notifications;

public record ListNotificationsQuery(bool UnreadOnly) : IRequest<IReadOnlyList<NotificationDto>>;
public record GetUnreadNotificationCountQuery() : IRequest<int>;
public record MarkNotificationReadCommand(Guid Id) : IRequest<Unit>;
public record MarkAllReadCommand() : IRequest<Unit>;
public record DeleteNotificationCommand(Guid Id) : IRequest<Unit>;
public record DeleteAllReadNotificationsCommand() : IRequest<Unit>;

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

public class GetUnreadNotificationCountHandler(IUnitOfWork uow, ICurrentUser current) : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    public async Task<int> Handle(GetUnreadNotificationCountQuery q, CancellationToken ct)
    {
        var userId = current.Id ?? throw new UnauthorizedAccessException();
        return await uow.Repo<Notification>().Query().Where(n => n.UserId == userId && !n.IsRead).CountAsync(ct);
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

public class DeleteNotificationHandler(IUnitOfWork uow, ICurrentUser current) : IRequestHandler<DeleteNotificationCommand, Unit>
{
    public async Task<Unit> Handle(DeleteNotificationCommand c, CancellationToken ct)
    {
        var userId = current.Id ?? throw new UnauthorizedAccessException();
        var n = await uow.Repo<Notification>().GetByIdAsync(c.Id, ct);
        if (n is null || n.UserId != userId) return Unit.Value;
        uow.Repo<Notification>().Remove(n);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public class DeleteAllReadNotificationsHandler(IUnitOfWork uow, ICurrentUser current) : IRequestHandler<DeleteAllReadNotificationsCommand, Unit>
{
    public async Task<Unit> Handle(DeleteAllReadNotificationsCommand _, CancellationToken ct)
    {
        var userId = current.Id ?? throw new UnauthorizedAccessException();
        var read = await uow.Repo<Notification>().Query().Where(n => n.UserId == userId && n.IsRead).ToListAsync(ct);
        foreach (var n in read) uow.Repo<Notification>().Remove(n);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

