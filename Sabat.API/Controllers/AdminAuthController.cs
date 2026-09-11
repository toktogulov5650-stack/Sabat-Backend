using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Sabat.API.Security;
using Sabat.Application.Admin;
using Sabat.Application.Common;

namespace Sabat.API.Controllers;

[ApiController]
[Route("api/admin/auth")]
public sealed class AdminAuthController(
    IAdminAuthService adminAuthService,
    IAdminTokenService tokenService,
    IOptions<AdminJwtOptions> jwtOptions,
    IWebHostEnvironment environment) : ControllerBase
{
    private readonly AdminJwtOptions _jwtOptions = jwtOptions.Value;

    [HttpPost("bootstrap")]
    [Authorize(Policy = AdminApiKeyOptions.ApiKeyPolicyName)]
    [ProducesResponseType<AdminUserDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AdminUserDto>> Bootstrap(
        BootstrapAdminRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminAuthService.BootstrapAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result.Admin);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("admin-login")]
    public async Task<ActionResult<AdminLoginResponse>> Login(
        AdminLoginRequest request,
        CancellationToken cancellationToken)
    {
        var authenticatedAdmin = await adminAuthService.AuthenticateAsync(request, cancellationToken);
        var issuedToken = tokenService.Issue(authenticatedAdmin);
        SetSessionCookies(issuedToken);

        return Ok(new AdminLoginResponse(
            authenticatedAdmin.Admin,
            issuedToken.ExpiresAt,
            issuedToken.CsrfToken));
    }

    [HttpGet("me")]
    [Authorize(Policy = AdminApiKeyOptions.SessionPolicyName)]
    public async Task<ActionResult<AdminUserDto>> Me(CancellationToken cancellationToken)
    {
        var result = await adminAuthService.GetSessionAsync(GetAdminId(), cancellationToken);
        return Ok(result.Admin);
    }

    [HttpPost("change-password")]
    [Authorize(Policy = AdminApiKeyOptions.SessionPolicyName)]
    public async Task<ActionResult<AdminLoginResponse>> ChangePassword(
        ChangeAdminPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var authenticatedAdmin = await adminAuthService.ChangePasswordAsync(
            GetAdminId(),
            request,
            cancellationToken);
        var issuedToken = tokenService.Issue(authenticatedAdmin);
        SetSessionCookies(issuedToken);

        return Ok(new AdminLoginResponse(
            authenticatedAdmin.Admin,
            issuedToken.ExpiresAt,
            issuedToken.CsrfToken));
    }

    [HttpPost("logout")]
    [Authorize(Policy = AdminApiKeyOptions.SessionPolicyName)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await adminAuthService.RevokeSessionsAsync(GetAdminId(), cancellationToken);
        DeleteSessionCookies();
        return NoContent();
    }

    private Guid GetAdminId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var adminId))
        {
            throw new UnauthorizedException("The administrator session is invalid.");
        }

        return adminId;
    }

    private void SetSessionCookies(IssuedAdminToken token)
    {
        var secure = !environment.IsDevelopment() || Request.IsHttps;
        var sameSite = environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None;
        var commonOptions = new CookieOptions
        {
            Secure = secure,
            SameSite = sameSite,
            Path = "/",
            Expires = token.ExpiresAt,
            IsEssential = true
        };

        Response.Cookies.Append(_jwtOptions.CookieName, token.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = commonOptions.Secure,
            SameSite = commonOptions.SameSite,
            Path = commonOptions.Path,
            Expires = commonOptions.Expires,
            IsEssential = true
        });
        Response.Cookies.Append(_jwtOptions.CsrfCookieName, token.CsrfToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = commonOptions.Secure,
            SameSite = commonOptions.SameSite,
            Path = commonOptions.Path,
            Expires = commonOptions.Expires,
            IsEssential = true
        });
    }

    private void DeleteSessionCookies()
    {
        var secure = !environment.IsDevelopment() || Request.IsHttps;
        var sameSite = environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None;
        var deleteOptions = new CookieOptions
        {
            Secure = secure,
            SameSite = sameSite,
            Path = "/"
        };

        Response.Cookies.Delete(_jwtOptions.CookieName, deleteOptions);
        Response.Cookies.Delete(_jwtOptions.CsrfCookieName, deleteOptions);
    }
}
