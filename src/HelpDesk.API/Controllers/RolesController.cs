using HelpDesk.Application.Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.API.Controllers;

[ApiController, Route("api/roles"), Authorize]
public class RolesController : ControllerBase
{
    // Role catalog + the permissions each one grants. Mirrors src/lib/auth/permissions.ts.
    private static readonly RoleDto[] Roles =
    [
        new("Admin", ["Ticket.View","Ticket.Create","Ticket.Edit","Ticket.Delete","Ticket.Assign","Comment.Internal","User.Manage","Department.Manage","Role.Manage"]),
        new("Manager", ["Ticket.View","Ticket.Create","Ticket.Edit","Ticket.Delete","Ticket.Assign","Comment.Internal","Department.Manage"]),
        new("Agent", ["Ticket.View","Ticket.Create","Ticket.Edit","Ticket.Assign","Comment.Internal"]),
        new("Customer", ["Ticket.View","Ticket.Create"]),
    ];

    [HttpGet] public IEnumerable<RoleDto> List() => Roles;
    [HttpGet("{name}")] public ActionResult<RoleDto> Get(string name) =>
        Roles.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) is { } r ? r : NotFound();
}

[ApiController, Route("api/permissions"), Authorize]
public class PermissionsController : ControllerBase
{
    private static readonly string[] All =
    [
        "Ticket.View","Ticket.Create","Ticket.Edit","Ticket.Delete","Ticket.Assign",
        "Comment.Internal","User.Manage","Department.Manage","Role.Manage",
    ];
    [HttpGet] public IEnumerable<string> List() => All;
}
