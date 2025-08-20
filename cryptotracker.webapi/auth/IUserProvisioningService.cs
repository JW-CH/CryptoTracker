// IUserProvisioningService.cs
using System.Security.Claims;

namespace cryptotracker.webapi.auth;

public interface IUserProvisioningService
{
    /// <summary>
    /// Ensure a domain user exists for the authenticated principal; returns the internal UserId.
    /// Idempotent: creates the user on first login, updates display data on subsequent logins.
    /// </summary>
    Task<Guid> UpsertFromOidcAsync(ClaimsPrincipal principal, CancellationToken ct = default);
}
