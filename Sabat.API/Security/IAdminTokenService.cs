using Sabat.Application.Admin;

namespace Sabat.API.Security;

public sealed record IssuedAdminToken(
    string Token,
    DateTimeOffset ExpiresAt,
    string CsrfToken);

public interface IAdminTokenService
{
    IssuedAdminToken Issue(AuthenticatedAdmin authenticatedAdmin);
}
