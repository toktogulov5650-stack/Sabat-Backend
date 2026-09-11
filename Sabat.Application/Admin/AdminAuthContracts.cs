namespace Sabat.Application.Admin;

public sealed record BootstrapAdminRequest(
    string Email,
    string DisplayName,
    string Password);

public sealed record AdminLoginRequest(
    string Email,
    string Password);

public sealed record ChangeAdminPasswordRequest(
    string CurrentPassword,
    string NewPassword);

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);

public sealed record AuthenticatedAdmin(
    AdminUserDto Admin,
    int SessionVersion);

public sealed record AdminLoginResponse(
    AdminUserDto Admin,
    DateTimeOffset ExpiresAt,
    string CsrfToken);

public interface IAdminAuthService
{
    Task<AuthenticatedAdmin> BootstrapAsync(
        BootstrapAdminRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthenticatedAdmin> AuthenticateAsync(
        AdminLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthenticatedAdmin> GetSessionAsync(
        Guid adminId,
        CancellationToken cancellationToken = default);

    Task<AuthenticatedAdmin> ChangePasswordAsync(
        Guid adminId,
        ChangeAdminPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task RevokeSessionsAsync(
        Guid adminId,
        CancellationToken cancellationToken = default);
}
