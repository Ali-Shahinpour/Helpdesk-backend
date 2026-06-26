using HelpDesk.Domain.Enums;

namespace HelpDesk.Application.Common.DTOs;

public record UserDto(Guid Id, string Email, string FullName, UserRole Role, Guid? DepartmentId, bool IsActive, DateTime CreatedAt);
public record DepartmentDto(Guid Id, string Name, string? Description, DateTime CreatedAt);
public record TicketDto(Guid Id, string Number, string Subject, string Description, TicketStatus Status, TicketPriority Priority,
    TicketCategory Category, Guid CustomerId, Guid? AssignedAgentId, Guid? DepartmentId, DateTime CreatedAt, DateTime UpdatedAt, DateTime? ClosedAt);
public record CommentDto(Guid Id, Guid TicketId, Guid AuthorId, string Body, bool IsInternal, DateTime CreatedAt);
public record AttachmentDto(Guid Id, Guid TicketId, string FileName, long Size, string ContentType, string Url, Guid UploadedById, DateTime CreatedAt);
public record ActivityDto(Guid Id, Guid TicketId, Guid ActorId, ActivityType Type, string? MetaJson, DateTime CreatedAt);
public record NotificationDto(Guid Id, Guid UserId, string Type, string Title, string? Body, Guid? TicketId, bool IsRead, DateTime CreatedAt);
public record DashboardStatsDto(int Total, int Open, int Closed, int Mine, Dictionary<string, int> ByStatus, Dictionary<string, int> ByPriority);
public record RoleDto(string Name, string[] Permissions);

public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserDto User);

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string FullName, string Password);
public record RefreshRequest(string? RefreshToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);

public record CreateTicketRequest(string Subject, string Description, TicketPriority Priority, TicketCategory Category, Guid? DepartmentId);
public record UpdateTicketRequest(string? Subject, string? Description, TicketStatus? Status, TicketPriority? Priority,
    TicketCategory? Category, Guid? AssignedAgentId, Guid? DepartmentId);
public record AddCommentRequest(string Body, bool IsInternal);

public record CreateUserRequest(string Email, string FullName, string Password, UserRole Role, Guid? DepartmentId, bool IsActive);
public record UpdateUserRequest(string? FullName, UserRole? Role, Guid? DepartmentId, bool? IsActive);
public record CreateDepartmentRequest(string Name, string? Description);
public record UpdateDepartmentRequest(string? Name, string? Description);
