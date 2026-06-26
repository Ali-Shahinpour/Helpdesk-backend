using HelpDesk.Domain.Enums;

namespace HelpDesk.Domain.Entities;

public abstract class BaseEntity
{
    //public Guid Id { get; set; } = Guid.NewGuid();
    //public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    //public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Customer;
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

public class Ticket : BaseEntity
{
    public string Number { get; set; } = string.Empty;          // TKT-1001
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.New;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketCategory Category { get; set; } = TicketCategory.General;
    public Guid CustomerId { get; set; }
    public User? Customer { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public User? AssignedAgent { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public DateTime? ClosedAt { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<ActivityEvent> Activities { get; set; } = new List<ActivityEvent>();
}

public class Comment : BaseEntity
{
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public Guid AuthorId { get; set; }
    public User? Author { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
}

public class Attachment : BaseEntity
{
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public Guid UploadedById { get; set; }
    public User? UploadedBy { get; set; }
}

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
}

public class ActivityEvent : BaseEntity
{
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public Guid ActorId { get; set; }
    public User? Actor { get; set; }
    public ActivityType Type { get; set; }
    public string? MetaJson { get; set; }
}

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Type { get; set; } = string.Empty;     // TicketAssigned, StatusChanged, CommentAdded, ...
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public Guid? TicketId { get; set; }
    public bool IsRead { get; set; }
}
