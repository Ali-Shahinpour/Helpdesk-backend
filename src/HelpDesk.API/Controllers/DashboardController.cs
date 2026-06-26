using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Features.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.API.Controllers;

[ApiController, Route("api/dashboard"), Authorize]
public class DashboardController(IMediator m) : ControllerBase
{
    [HttpGet("stats")] public Task<DashboardStatsDto> Stats() => m.Send(new GetDashboardStatsQuery());
    [HttpGet("activity")] public Task<IReadOnlyList<ActivityDto>> Activity([FromQuery] int limit = 10)
        => m.Send(new GetRecentActivityQuery(limit));
}
