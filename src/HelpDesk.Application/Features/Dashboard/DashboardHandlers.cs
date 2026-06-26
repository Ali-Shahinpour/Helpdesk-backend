using AutoMapper;
using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Common.Interfaces;
using HelpDesk.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.Features.Dashboard;

public record GetDashboardStatsQuery() : IRequest<DashboardStatsDto>;
public record GetRecentActivityQuery(int Limit) : IRequest<IReadOnlyList<ActivityDto>>;

public class GetDashboardStatsHandler(IUnitOfWork uow, ICurrentUser current) : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery _, CancellationToken ct)
    {
        var userId = current.Id ?? throw new UnauthorizedAccessException();
        var role = current.Role ?? UserRole.Customer;
        var baseQ = uow.Tickets.Query();
        if (role == UserRole.Customer) baseQ = baseQ.Where(t => t.CustomerId == userId);
        var tickets = await baseQ.Select(t => new { t.Status, t.Priority, t.CustomerId, t.AssignedAgentId }).ToListAsync(ct);

        var mine = role == UserRole.Customer
            ? tickets.Count(t => t.CustomerId == userId)
            : tickets.Count(t => t.AssignedAgentId == userId);

        return new DashboardStatsDto(
            tickets.Count,
            tickets.Count(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved),
            tickets.Count(t => t.Status == TicketStatus.Closed),
            mine,
            tickets.GroupBy(t => t.Status.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            tickets.GroupBy(t => t.Priority.ToString()).ToDictionary(g => g.Key, g => g.Count())
        );
    }
}

public class GetRecentActivityHandler(IUnitOfWork uow, IMapper map) : IRequestHandler<GetRecentActivityQuery, IReadOnlyList<ActivityDto>>
{
    public async Task<IReadOnlyList<ActivityDto>> Handle(GetRecentActivityQuery q, CancellationToken ct)
    {
        var list = await uow.Repo<Domain.Entities.ActivityEvent>().Query()
            .OrderByDescending(a => a.CreatedAt).Take(Math.Clamp(q.Limit, 1, 100)).ToListAsync(ct);
        return map.Map<List<ActivityDto>>(list);
    }
}
