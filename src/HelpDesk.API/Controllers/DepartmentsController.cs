using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Features.Departments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.API.Controllers;

[ApiController, Route("api/departments"), Authorize]
public class DepartmentsController(IMediator m) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<DepartmentDto>> List() => m.Send(new ListDepartmentsQuery());

    [HttpPost, Authorize(Roles = "Admin,Manager")]
    public Task<DepartmentDto> Create([FromBody] CreateDepartmentRequest r) => m.Send(new CreateDepartmentCommand(r));

    [HttpPut("{id:guid}"), Authorize(Roles = "Admin,Manager")]
    public Task<DepartmentDto> Update(Guid id, [FromBody] UpdateDepartmentRequest r) => m.Send(new UpdateDepartmentCommand(id, r));

    [HttpDelete("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id) { await m.Send(new DeleteDepartmentCommand(id)); return NoContent(); }
}
