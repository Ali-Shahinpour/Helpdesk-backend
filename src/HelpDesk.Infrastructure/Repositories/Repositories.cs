using System.Linq.Expressions;
using HelpDesk.Application.Common.Interfaces;
using HelpDesk.Domain.Entities;
using HelpDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HelpDesk.Infrastructure.Repositories;

public class Repository<T>(AppDbContext db) : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext Db = db;
    protected readonly DbSet<T> Set = db.Set<T>();
    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) => Set.FirstOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => predicate is null ? await Set.ToListAsync(ct) : await Set.Where(predicate).ToListAsync(ct);
    public IQueryable<T> Query() => Set.AsQueryable();
    public Task AddAsync(T entity, CancellationToken ct = default) => Set.AddAsync(entity, ct).AsTask();
    public void Update(T entity) => Set.Update(entity);
    public void Remove(T entity) => Set.Remove(entity);
}

public class TicketRepository(AppDbContext db) : Repository<Ticket>(db), ITicketRepository
{
    public async Task<string> NextTicketNumberAsync(CancellationToken ct = default)
    {
        var count = await Db.Tickets.CountAsync(ct);
        return $"TKT-{1000 + count + 1}";
    }
    public Task<Ticket?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
        => Db.Tickets.Include(t => t.Comments).Include(t => t.Attachments).Include(t => t.Activities).FirstOrDefaultAsync(t => t.Id == id, ct);
}

public class UserRepository(AppDbContext db) : Repository<User>(db), IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
        => Db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower(), ct);
}

public class UnitOfWork(AppDbContext db, ITicketRepository tickets, IUserRepository users) : IUnitOfWork
{
    private IDbContextTransaction? _tx;
    public ITicketRepository Tickets { get; } = tickets;
    public IUserRepository Users { get; } = users;
    public IRepository<TEntity> Repo<TEntity>() where TEntity : BaseEntity => new Repository<TEntity>(db);
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
    public async Task BeginTransactionAsync(CancellationToken ct = default) => _tx = await db.Database.BeginTransactionAsync(ct);
    public async Task CommitAsync(CancellationToken ct = default) { if (_tx is not null) { await _tx.CommitAsync(ct); await _tx.DisposeAsync(); _tx = null; } }
    public async Task RollbackAsync(CancellationToken ct = default) { if (_tx is not null) { await _tx.RollbackAsync(ct); await _tx.DisposeAsync(); _tx = null; } }
    public ValueTask DisposeAsync() => db.DisposeAsync();
}
