using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e => {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(120).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            e.HasOne(x => x.Department).WithMany(d => d.Users).HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        });
        b.Entity<Department>(e => {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
        });
        b.Entity<Ticket>(e => {
            e.HasIndex(x => x.Number).IsUnique();
            e.Property(x => x.Number).HasMaxLength(20).IsRequired();
            e.Property(x => x.Subject).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(8000).IsRequired();
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AssignedAgent).WithMany().HasForeignKey(x => x.AssignedAgentId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Department).WithMany(d => d.Tickets).HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Comments).WithOne(c => c.Ticket).HasForeignKey(c => c.TicketId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Attachments).WithOne(a => a.Ticket).HasForeignKey(a => a.TicketId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Activities).WithOne(a => a.Ticket).HasForeignKey(a => a.TicketId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<Comment>(e => { e.Property(x => x.Body).HasMaxLength(5000).IsRequired(); });
        b.Entity<Attachment>(e => {
            e.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
            e.Property(x => x.StoragePath).HasMaxLength(500).IsRequired();
        });
        //b.Entity<RefreshToken>(e => {
        //    e.HasIndex(x => x.Token).IsUnique();
        //    e.Property(x => x.Token).HasMaxLength(200).IsRequired();
        //    e.HasOne(x => x.User).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        //});
        b.Entity<RefreshToken>(e => {
            e.HasIndex(x => x.Token).IsUnique();
            e.Property(x => x.Token).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.User).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Ignore(x => x.IsActive);
        });

        b.Entity<ActivityEvent>(e => { e.Property(x => x.MetaJson).HasMaxLength(2000); });
        b.Entity<Notification>(e => {
            e.Property(x => x.Type).HasMaxLength(60).IsRequired();
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Body).HasMaxLength(1000);
            e.HasIndex(x => new { x.UserId, x.IsRead });
        });

        // ----- Seed -----
        //var techId = Guid.Parse("11111111-aaaa-aaaa-aaaa-111111111111");
        //var billId = Guid.Parse("22222222-aaaa-aaaa-aaaa-222222222222");
        //b.Entity<Department>().HasData(
        //    new Department { Id = techId, Name = "Technical Support", Description = "Product & technical issues" },
        //    new Department { Id = billId, Name = "Billing", Description = "Invoices & payments" });

        //const string hash = "$2a$11$JqQ8FbZcWQ3l7p4dDp4Tn.j2C0i8KQ.dQwK7uOmCJWvKjvY4lqDZK";
        //b.Entity<User>().HasData(
        //    new User { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), Email = "admin@helix.dev", FullName = "Ada Admin", PasswordHash = hash, Role = UserRole.Admin },
        //    new User { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"), Email = "manager@helix.dev", FullName = "Mark Manager", PasswordHash = hash, Role = UserRole.Manager, DepartmentId = techId },
        //    new User { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"), Email = "agent@helix.dev", FullName = "Anya Agent", PasswordHash = hash, Role = UserRole.Agent, DepartmentId = techId },
        //    new User { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004"), Email = "customer@helix.dev", FullName = "Cara Customer", PasswordHash = hash, Role = UserRole.Customer });
        // ----- Seed -----
        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var techId = Guid.Parse("11111111-aaaa-aaaa-aaaa-111111111111");
        var billId = Guid.Parse("22222222-aaaa-aaaa-aaaa-222222222222");

        b.Entity<Department>().HasData(
            new Department { Id = techId, Name = "Technical Support", Description = "Product & technical issues", CreatedAt = seededAt, UpdatedAt = seededAt },
            new Department { Id = billId, Name = "Billing", Description = "Invoices & payments", CreatedAt = seededAt, UpdatedAt = seededAt }
        );

        const string hash = "$2a$11$JqQ8FbZcWQ3l7p4dDp4Tn.j2C0i8KQ.dQwK7uOmCJWvKjvY4lqDZK";

        b.Entity<User>().HasData(
            new User { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), Email = "admin@helix.dev", FullName = "Ada Admin", PasswordHash = hash, Role = UserRole.Admin, IsActive = true, CreatedAt = seededAt, UpdatedAt = seededAt },
            new User { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"), Email = "manager@helix.dev", FullName = "Mark Manager", PasswordHash = hash, Role = UserRole.Manager, DepartmentId = techId, IsActive = true, CreatedAt = seededAt, UpdatedAt = seededAt },
            new User { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"), Email = "agent@helix.dev", FullName = "Anya Agent", PasswordHash = hash, Role = UserRole.Agent, DepartmentId = techId, IsActive = true, CreatedAt = seededAt, UpdatedAt = seededAt },
            new User { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004"), Email = "customer@helix.dev", FullName = "Cara Customer", PasswordHash = hash, Role = UserRole.Customer, IsActive = true, CreatedAt = seededAt, UpdatedAt = seededAt }
        );

    }
}
