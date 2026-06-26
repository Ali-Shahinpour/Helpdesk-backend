using System.Security.Cryptography;
using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Common.Interfaces;
using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.Features.Auth;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
public record RegisterCommand(string Email, string FullName, string Password) : IRequest<AuthResponse>;
public record RefreshCommand(string RefreshToken) : IRequest<AuthResponse>;
public record LogoutCommand(string RefreshToken) : IRequest<Unit>;
public record ForgotPasswordCommand(string Email) : IRequest<string>;          // returns reset token
public record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Unit>;

public class AuthHandlers(IUnitOfWork uow, IJwtTokenService jwt, IPasswordHasher hasher)
    : IRequestHandler<LoginCommand, AuthResponse>,
      IRequestHandler<RegisterCommand, AuthResponse>,
      IRequestHandler<RefreshCommand, AuthResponse>,
      IRequestHandler<LogoutCommand, Unit>
{
    public async Task<AuthResponse> Handle(LoginCommand r, CancellationToken ct)
    {
        var user = await uow.Users.FindByEmailAsync(r.Email, ct)
                   ?? throw new UnauthorizedAccessException("Invalid credentials");
        if (!user.IsActive) throw new UnauthorizedAccessException("Account disabled");
        if (!hasher.Verify(r.Password, user.PasswordHash)) throw new UnauthorizedAccessException("Invalid credentials");
        return await IssueAsync(user, ct);
    }

    public async Task<AuthResponse> Handle(RegisterCommand r, CancellationToken ct)
    {
        if (await uow.Users.FindByEmailAsync(r.Email, ct) is not null)
            throw new InvalidOperationException("Email already registered");
        var user = new User
        {
            Email = r.Email.Trim().ToLowerInvariant(),
            FullName = r.FullName.Trim(),
            PasswordHash = hasher.Hash(r.Password),
            Role = UserRole.Customer,
        };
        await uow.Users.AddAsync(user, ct);
        await uow.SaveChangesAsync(ct);
        return await IssueAsync(user, ct);
    }

    public async Task<AuthResponse> Handle(RefreshCommand r, CancellationToken ct)
    {
        var token = await uow.Repo<RefreshToken>().Query()
            .Include(t => t.User).FirstOrDefaultAsync(t => t.Token == r.RefreshToken, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token");
        if (!token.IsActive) throw new UnauthorizedAccessException("Refresh token expired");
        token.RevokedAt = DateTime.UtcNow;
        var newRefresh = jwt.GenerateRefreshToken();
        token.ReplacedByToken = newRefresh;
        token.User!.RefreshTokens.Add(new RefreshToken { Token = newRefresh, ExpiresAt = DateTime.UtcNow.AddDays(7), UserId = token.UserId });
        await uow.SaveChangesAsync(ct);
        var (access, exp) = jwt.IssueAccessToken(token.User);
        return new AuthResponse(access, newRefresh, exp, token.User.ToDto());
    }

    public async Task<Unit> Handle(LogoutCommand r, CancellationToken ct)
    {
        var token = await uow.Repo<RefreshToken>().Query().FirstOrDefaultAsync(t => t.Token == r.RefreshToken, ct);
        if (token is { RevokedAt: null }) { token.RevokedAt = DateTime.UtcNow; await uow.SaveChangesAsync(ct); }
        return Unit.Value;
    }

    private async Task<AuthResponse> IssueAsync(User user, CancellationToken ct)
    {
        var (access, exp) = jwt.IssueAccessToken(user);
        var refresh = jwt.GenerateRefreshToken();
        user.RefreshTokens.Add(new RefreshToken { Token = refresh, ExpiresAt = DateTime.UtcNow.AddDays(7), UserId = user.Id });
        await uow.SaveChangesAsync(ct);
        return new AuthResponse(access, refresh, exp, user.ToDto());
    }
}

internal static class UserMappingExtensions
{
    public static UserDto ToDto(this User u) => new(u.Id, u.Email, u.FullName, u.Role, u.DepartmentId, u.IsActive, u.CreatedAt);
}
