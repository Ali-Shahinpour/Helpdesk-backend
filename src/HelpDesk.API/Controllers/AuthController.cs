using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Common.Interfaces;
using HelpDesk.Application.Features.Auth;
using HelpDesk.Application.Features.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.API.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(IMediator m, ICurrentUser current, IWebHostEnvironment env) : ControllerBase
{
    private const string RefreshCookie = "hd_refresh";

    private void SetRefreshCookie(string token, DateTime expires)
    {
        Response.Cookies.Append(RefreshCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsDevelopment() ? true : true, // always Secure (https only)
            SameSite = SameSiteMode.None,                // cross-site (5173 -> 5001) needs None+Secure
            Expires = expires,
            Path = "/api/auth",
        });
    }

    private void ClearRefreshCookie() => Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = "/api/auth" });

    private object Shape(AuthResponse a)
    {
        SetRefreshCookie(a.RefreshToken, DateTime.UtcNow.AddDays(7));
        return new { accessToken = a.AccessToken, expiresAt = a.ExpiresAt, user = a.User };
    }

    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest r)
        => Ok(Shape(await m.Send(new LoginCommand(r.Email, r.Password))));

    [HttpPost("register"), AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest r)
        => Ok(Shape(await m.Send(new RegisterCommand(r.Email, r.FullName, r.Password))));

    [HttpPost("refresh"), AllowAnonymous]
    public async Task<IActionResult> Refresh()
    {
        var token = Request.Cookies[RefreshCookie];
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        var result = await m.Send(new RefreshCommand(token));
        return Ok(Shape(result));
    }

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies[RefreshCookie];
        if (!string.IsNullOrEmpty(token)) await m.Send(new LogoutCommand(token));
        ClearRefreshCookie();
        return NoContent();
    }

    [HttpGet("me"), Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        var id = current.Id ?? throw new UnauthorizedAccessException();
        return await m.Send(new GetUserQuery(id));
    }

    [HttpPost("forgot-password"), AllowAnonymous]
    public async Task<IActionResult> Forgot([FromBody] ForgotPasswordRequest r)
    {
        var token = await m.Send(new ForgotPasswordCommand(r.Email));
        return Ok(new { token });
    }

    [HttpPost("reset-password"), AllowAnonymous]
    public async Task<IActionResult> Reset([FromBody] ResetPasswordRequest r)
    { await m.Send(new ResetPasswordCommand(r.Token, r.NewPassword)); return NoContent(); }
}
