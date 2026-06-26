using System.Linq.Expressions;
using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;

namespace HelpDesk.Application.Common.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
    IQueryable<T> Query();
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}

public interface ITicketRepository : IRepository<Ticket>
{
    Task<string> NextTicketNumberAsync(CancellationToken ct = default);
    Task<Ticket?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
}

public interface IUnitOfWork : IAsyncDisposable
{
    IRepository<TEntity> Repo<TEntity>() where TEntity : BaseEntity;
    ITicketRepository Tickets { get; }
    IUserRepository Users { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}

public interface ICurrentUser
{
    Guid? Id { get; }
    string? Email { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
}

public interface IJwtTokenService
{
    (string AccessToken, DateTime ExpiresAt) IssueAccessToken(User user);
    string GenerateRefreshToken();
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface INotificationPublisher
{
    Task TicketCreatedAsync(Ticket t, CancellationToken ct = default);
    Task TicketUpdatedAsync(Ticket t, CancellationToken ct = default);
    Task TicketAssignedAsync(Ticket t, Guid? previousAgentId, CancellationToken ct = default);
    Task CommentAddedAsync(Comment c, CancellationToken ct = default);
    Task NotifyUserAsync(Guid userId, string type, string title, string? body, Guid? ticketId, CancellationToken ct = default);
}

public interface IFileStorage
{
    Task<string> SaveAsync(Guid ticketId, string fileName, byte[] content, CancellationToken ct = default);
    Task<byte[]> ReadAsync(string storagePath, CancellationToken ct = default);
    Task DeleteAsync(string storagePath, CancellationToken ct = default);
}
