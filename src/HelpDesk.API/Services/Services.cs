using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Common.Interfaces;
using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace HelpDesk.API.Services;

public class CurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    private ClaimsPrincipal? Principal => http.HttpContext?.User;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
    public Guid? Id => Guid.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var g) ? g : null;
    public string? Email => Principal?.FindFirstValue(JwtRegisteredClaimNames.Email);
    public UserRole? Role => Enum.TryParse<UserRole>(Principal?.FindFirstValue(ClaimTypes.Role), out var r) ? r : null;
}

public class SignalRNotificationPublisher(IHubContext<Hubs.NotificationsHub> hub, IServiceProvider sp) : INotificationPublisher
{
    public Task TicketCreatedAsync(Ticket t, CancellationToken ct = default)
        => hub.Clients.All.SendAsync("TicketCreated", new { t.Id, t.Number, t.Subject, t.CustomerId, t.AssignedAgentId, t.Status, t.Priority }, ct);

    public Task TicketUpdatedAsync(Ticket t, CancellationToken ct = default)
        => hub.Clients.All.SendAsync("TicketUpdated", new { t.Id, t.Number, t.Status, t.Priority, t.AssignedAgentId }, ct);

    public async Task TicketAssignedAsync(Ticket t, Guid? previousAgentId, CancellationToken ct = default)
    {
        await hub.Clients.All.SendAsync("TicketAssigned", new { t.Id, t.Number, t.AssignedAgentId, previousAgentId }, ct);
        if (t.AssignedAgentId is { } agent)
            await NotifyUserAsync(agent, "TicketAssigned", $"Ticket {t.Number} assigned to you", t.Subject, t.Id, ct);
    }

    public async Task CommentAddedAsync(Comment c, CancellationToken ct = default)
    {
        await hub.Clients.All.SendAsync("CommentAdded", new { c.TicketId, c.AuthorId, c.IsInternal, c.Body }, ct);
        // Notify ticket customer + assignee on public comments.
        if (c.IsInternal) return;
        using var scope = sp.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var t = await uow.Tickets.GetByIdAsync(c.TicketId, ct);
        if (t is null) return;
        var targets = new[] { t.CustomerId, t.AssignedAgentId ?? Guid.Empty }.Where(x => x != Guid.Empty && x != c.AuthorId).Distinct();
        foreach (var u in targets)
            await NotifyUserAsync(u, "CommentAdded", $"New comment on {t.Number}", c.Body[..Math.Min(120, c.Body.Length)], t.Id, ct);
    }

    public async Task NotifyUserAsync(Guid userId, string type, string title, string? body, Guid? ticketId, CancellationToken ct = default)
    {
        using var scope = sp.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var n = new Notification { UserId = userId, Type = type, Title = title, Body = body, TicketId = ticketId };
        await uow.Repo<Notification>().AddAsync(n, ct);
        await uow.SaveChangesAsync(ct);
        await hub.Clients.Group($"user:{userId}").SendAsync("NotificationReceived",
            new NotificationDto(n.Id, n.UserId, n.Type, n.Title, n.Body, n.TicketId, n.IsRead, n.CreatedAt), ct);
    }
}

public class LocalFileStorage(IWebHostEnvironment env) : IFileStorage
{
    private string Root => Path.Combine(env.ContentRootPath, "App_Data", "attachments");

    public async Task<string> SaveAsync(Guid ticketId, string fileName, byte[] content, CancellationToken ct = default)
    {
        var dir = Path.Combine(Root, ticketId.ToString());
        Directory.CreateDirectory(dir);
        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var full = Path.Combine(dir, safeName);
        await File.WriteAllBytesAsync(full, content, ct);
        return Path.Combine(ticketId.ToString(), safeName).Replace('\\', '/');
    }
    public Task<byte[]> ReadAsync(string storagePath, CancellationToken ct = default)
        => File.ReadAllBytesAsync(Path.Combine(Root, storagePath), ct);
    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    { var p = Path.Combine(Root, storagePath); if (File.Exists(p)) File.Delete(p); return Task.CompletedTask; }
}
