using AutoMapper;
using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Common.Interfaces;
using HelpDesk.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.Features.Departments;

public record ListDepartmentsQuery() : IRequest<IReadOnlyList<DepartmentDto>>;
public record CreateDepartmentCommand(CreateDepartmentRequest Data) : IRequest<DepartmentDto>;
public record UpdateDepartmentCommand(Guid Id, UpdateDepartmentRequest Data) : IRequest<DepartmentDto>;
public record DeleteDepartmentCommand(Guid Id) : IRequest<Unit>;

public class ListDepartmentsHandler(IUnitOfWork uow, IMapper map) : IRequestHandler<ListDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    public async Task<IReadOnlyList<DepartmentDto>> Handle(ListDepartmentsQuery _, CancellationToken ct)
    {
        var list = await uow.Repo<Department>().Query().OrderBy(d => d.Name).ToListAsync(ct);
        return map.Map<List<DepartmentDto>>(list);
    }
}

public class CreateDepartmentHandler(IUnitOfWork uow, IMapper map) : IRequestHandler<CreateDepartmentCommand, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(CreateDepartmentCommand c, CancellationToken ct)
    {
        var d = new Department { Name = c.Data.Name, Description = c.Data.Description };
        await uow.Repo<Department>().AddAsync(d, ct);
        await uow.SaveChangesAsync(ct);
        return map.Map<DepartmentDto>(d);
    }
}

public class UpdateDepartmentHandler(IUnitOfWork uow, IMapper map) : IRequestHandler<UpdateDepartmentCommand, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(UpdateDepartmentCommand c, CancellationToken ct)
    {
        var d = await uow.Repo<Department>().GetByIdAsync(c.Id, ct) ?? throw new KeyNotFoundException();
        if (c.Data.Name is { } n) d.Name = n;
        if (c.Data.Description is { } x) d.Description = x;
        d.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
        return map.Map<DepartmentDto>(d);
    }
}

public class DeleteDepartmentHandler(IUnitOfWork uow) : IRequestHandler<DeleteDepartmentCommand, Unit>
{
    public async Task<Unit> Handle(DeleteDepartmentCommand c, CancellationToken ct)
    {
        var d = await uow.Repo<Department>().GetByIdAsync(c.Id, ct) ?? throw new KeyNotFoundException();
        uow.Repo<Department>().Remove(d);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
