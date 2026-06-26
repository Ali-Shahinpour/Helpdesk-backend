using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Features.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.API.Controllers;

[ApiController, Route("api/users"), Authorize]
public class UsersController(IMediator m) : ControllerBase
{
    [HttpGet, Authorize(Roles = "Admin,Manager,Agent")]
    public Task<IReadOnlyList<UserDto>> List() => m.Send(new ListUsersQuery());

    [HttpGet("{id:guid}"), Authorize(Roles = "Admin,Manager,Agent")]
    public Task<UserDto> Get(Guid id) => m.Send(new GetUserQuery(id));

    [HttpPost, Authorize(Roles = "Admin")]
    public Task<UserDto> Create([FromBody] CreateUserRequest r) => m.Send(new CreateUserCommand(r));

    [HttpPut("{id:guid}"), Authorize(Roles = "Admin")]
    public Task<UserDto> Update(Guid id, [FromBody] UpdateUserRequest r) => m.Send(new UpdateUserCommand(id, r));

    [HttpDelete("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id) { await m.Send(new DeleteUserCommand(id)); return NoContent(); }
}
