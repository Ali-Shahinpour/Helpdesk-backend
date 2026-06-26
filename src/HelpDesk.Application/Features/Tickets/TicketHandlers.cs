using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Common.Interfaces;
using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using INotificationPublisher = HelpDesk.Application.Common.Interfaces.INotificationPublisher;


namespace HelpDesk.Application.Features.Tickets;

// ---------- Create ----------
public record CreateTicketCommand(string Subject, string Description, TicketPriority Priority,
    TicketCategory Category, Guid? DepartmentId) : IRequest<TicketDto>;

public class CreateTicketHandler(IUnitOfWork uow, ICurrentUser current, INotificationPublisher pub)
    : IRequestHandler<CreateTicketCommand, TicketDto>
{
    public async Task<TicketDto> Handle(CreateTicketCommand req, CancellationToken ct)
    {
        if (current.Id is null) throw new UnauthorizedAccessException();
        var ticket = new Ticket
        {
            Number = await uow.Tickets.NextTicketNumberAsync(ct),
            Subject = req.Subject, Description = req.Description,
            Priority = req.Priority, Category = req.Category,
            DepartmentId = req.DepartmentId, CustomerId = current.Id.Value, Status = TicketStatus.New,
        };
        ticket.Activities.Add(new ActivityEvent { TicketId = ticket.Id, ActorId = current.Id.Value, Type = ActivityType.Created });
        await uow.Tickets.AddAsync(ticket, ct);
        await uow.SaveChangesAsync(ct);
        await pub.TicketCreatedAsync(ticket, ct);
        return ticket.ToDto();
    }
}

// ---------- Update ----------
public record UpdateTicketCommand(Guid Id, UpdateTicketRequest Data) : IRequest<TicketDto>;
public class UpdateTicketHandler(IUnitOfWork uow, ICurrentUser current, INotificationPublisher pub)
    : IRequestHandler<UpdateTicketCommand, TicketDto>
{
    public async Task<TicketDto> Handle(UpdateTicketCommand req, CancellationToken ct)
    {
        var t = await uow.Tickets.GetByIdAsync(req.Id, ct) ?? throw new KeyNotFoundException();
        var actor = current.Id ?? throw new UnauthorizedAccessException();

        if (req.Data.Subject is { } s) t.Subject = s;
        if (req.Data.Description is { } d) t.Description = d;
        if (req.Data.Priority is { } p) t.Priority = p;
        if (req.Data.Category is { } c) t.Category = c;
        if (req.Data.DepartmentId is { } dep) t.DepartmentId = dep;
        if (req.Data.AssignedAgentId.HasValue && req.Data.AssignedAgentId != t.AssignedAgentId)
        {
            t.AssignedAgentId = req.Data.AssignedAgentId;
            t.Activities.Add(new ActivityEvent { TicketId = t.Id, ActorId = actor, Type = ActivityType.Assigned });
        }
        if (req.Data.Status is { } st && st != t.Status)
        {
            var prev = t.Status; t.Status = st;
            if (st == TicketStatus.Closed) { t.ClosedAt = DateTime.UtcNow; t.Activities.Add(new ActivityEvent { TicketId = t.Id, ActorId = actor, Type = ActivityType.Closed }); }
            else if (prev == TicketStatus.Closed) { t.ClosedAt = null; t.Activities.Add(new ActivityEvent { TicketId = t.Id, ActorId = actor, Type = ActivityType.Reopened }); }
            else t.Activities.Add(new ActivityEvent { TicketId = t.Id, ActorId = actor, Type = ActivityType.StatusChanged });
        }
        t.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
        await pub.TicketUpdatedAsync(t, ct);
        return t.ToDto();
    }
}

// ---------- Delete ----------
public record DeleteTicketCommand(Guid Id) : IRequest<Unit>;
public class DeleteTicketHandler(IUnitOfWork uow) : IRequestHandler<DeleteTicketCommand, Unit>
{
    public async Task<Unit> Handle(DeleteTicketCommand req, CancellationToken ct)
    {
        var t = await uow.Tickets.GetByIdAsync(req.Id, ct) ?? throw new KeyNotFoundException();
        uow.Tickets.Remove(t);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ---------- Queries ----------
public record GetTicketQuery(Guid Id) : IRequest<TicketDto>;
public class GetTicketHandler(IUnitOfWork uow) : IRequestHandler<GetTicketQuery, TicketDto>
{
    public async Task<TicketDto> Handle(GetTicketQuery req, CancellationToken ct)
        => (await uow.Tickets.GetByIdAsync(req.Id, ct) ?? throw new KeyNotFoundException()).ToDto();
}

public record ListTicketsQuery(TicketStatus? Status, Guid? AssignedAgentId, Guid? CustomerId, string? Q) : IRequest<IReadOnlyList<TicketDto>>;
public class ListTicketsHandler(IUnitOfWork uow) : IRequestHandler<ListTicketsQuery, IReadOnlyList<TicketDto>>
{
    public async Task<IReadOnlyList<TicketDto>> Handle(ListTicketsQuery req, CancellationToken ct)
    {
        var q = uow.Tickets.Query();
        if (req.Status is { } s) q = q.Where(t => t.Status == s);
        if (req.AssignedAgentId is { } a) q = q.Where(t => t.AssignedAgentId == a);
        if (req.CustomerId is { } c) q = q.Where(t => t.CustomerId == c);
        if (!string.IsNullOrWhiteSpace(req.Q))
        {
            var term = req.Q.Trim();
            q = q.Where(t => t.Subject.Contains(term) || t.Number.Contains(term));
        }
        var list = await q.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
        return list.Select(TicketMappingExtensions.ToDto).ToList();
    }
}

internal static class TicketMappingExtensions
{
    public static TicketDto ToDto(this Ticket t) => new(
        t.Id, t.Number, t.Subject, t.Description, t.Status, t.Priority, t.Category,
        t.CustomerId, t.AssignedAgentId, t.DepartmentId, t.CreatedAt, t.UpdatedAt, t.ClosedAt);
}
