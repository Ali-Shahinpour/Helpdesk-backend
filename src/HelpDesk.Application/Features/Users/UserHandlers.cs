using AutoMapper;
using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Common.Interfaces;
using HelpDesk.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.Features.Users;

public record ListUsersQuery() : IRequest<IReadOnlyList<UserDto>>;
public record GetUserQuery(Guid Id) : IRequest<UserDto>;
public record CreateUserCommand(CreateUserRequest Data) : IRequest<UserDto>;
public record UpdateUserCommand(Guid Id, UpdateUserRequest Data) : IRequest<UserDto>;
public record DeleteUserCommand(Guid Id) : IRequest<Unit>;

public class ListUsersHandler(IUnitOfWork uow, IMapper map) : IRequestHandler<ListUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(ListUsersQuery _, CancellationToken ct)
    {
        var list = await uow.Users.Query().OrderBy(u => u.FullName).ToListAsync(ct);
        return map.Map<List<UserDto>>(list);
    }
}

public class GetUserHandler(IUnitOfWork uow, IMapper map) : IRequestHandler<GetUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserQuery q, CancellationToken ct)
        => map.Map<UserDto>(await uow.Users.GetByIdAsync(q.Id, ct) ?? throw new KeyNotFoundException());
}

public class CreateUserHandler(IUnitOfWork uow, IPasswordHasher hasher, IMapper map) : IRequestHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateUserCommand c, CancellationToken ct)
    {
        if (await uow.Users.FindByEmailAsync(c.Data.Email, ct) is not null)
            throw new InvalidOperationException("Email already in use.");
        var user = new User
        {
            Email = c.Data.Email, FullName = c.Data.FullName, Role = c.Data.Role,
            DepartmentId = c.Data.DepartmentId, IsActive = c.Data.IsActive,
            PasswordHash = hasher.Hash(c.Data.Password),
        };
        await uow.Users.AddAsync(user, ct);
        await uow.SaveChangesAsync(ct);
        return map.Map<UserDto>(user);
    }
}

public class UpdateUserHandler(IUnitOfWork uow, IMapper map) : IRequestHandler<UpdateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserCommand c, CancellationToken ct)
    {
        var u = await uow.Users.GetByIdAsync(c.Id, ct) ?? throw new KeyNotFoundException();
        if (c.Data.FullName is { } n) u.FullName = n;
        if (c.Data.Role is { } r) u.Role = r;
        if (c.Data.DepartmentId is { } d) u.DepartmentId = d;
        if (c.Data.IsActive is { } a) u.IsActive = a;
        u.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
        return map.Map<UserDto>(u);
    }
}

public class DeleteUserHandler(IUnitOfWork uow) : IRequestHandler<DeleteUserCommand, Unit>
{
    public async Task<Unit> Handle(DeleteUserCommand c, CancellationToken ct)
    {
        var u = await uow.Users.GetByIdAsync(c.Id, ct) ?? throw new KeyNotFoundException();
        uow.Users.Remove(u);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
