using cryptotracker.database.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace cryptotracker.webapi.auth;

public class UserProvisioningService : IUserProvisioningService
{
    private readonly DatabaseContext _db;

    public UserProvisioningService(DatabaseContext db) => _db = db;

    public async Task<Guid> UpsertFromOidcAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        // 1) Extract stable identifier and display info from claims
        //    Pocket ID: use 'sub' as the stable subject; name/email best effort.
        var authProviderId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Missing 'sub' claim from OIDC provider.");

        var userName = principal.FindFirstValue(ClaimTypes.Name)
                          ?? principal.FindFirstValue("preferred_username")
                          ?? throw new InvalidOperationException("Missing 'name' or 'preferred_username' claim from OIDC provider.");

        var displayName = principal.FindFirstValue("name")
                          ?? "User";

        var email = principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue("email")
                    ?? null; // email is optional, but can be useful for notifications

        // Try find existing user
        var user = await _db.Users.FirstOrDefaultAsync(u => u.AuthProviderId == authProviderId, ct);

        if (user is null)
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == userName, ct);
            if (user is not null)
            {
                Console.WriteLine($"User with name '{userName}' exists but not with auth provider ID '{authProviderId}'. Updating auth provider ID.");
                // If user exists by name but not by auth provider, update auth provider ID
                user.AuthProviderId = authProviderId;
                user.DisplayName = displayName;
                user.Email = email;
                user.LastLoginAt = DateTime.UtcNow;
            }
            else
            {
                Console.WriteLine($"Creating new user with auth provider ID '{authProviderId}' and name '{userName}'.");
                // Create new user
                user = new User
                {
                    UserId = Guid.NewGuid(),
                    UserName = userName,
                    AuthProviderId = authProviderId,
                    DisplayName = displayName,
                    Email = email,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };

                _db.Users.Add(user);
            }

            if (!_db.Users.Any())
            {
                user.IsAdmin = true;
            }
        }
        else
        {
            Console.WriteLine($"Found existing user with auth provider ID '{authProviderId}'.");
            // 4) Update display info on subsequent logins (do not change identity key)
            user.DisplayName = displayName;
            user.Email = email;
            user.LastLoginAt = DateTime.UtcNow;
        }

        // 5) Save with uniqueness guarantee (unique index on AuthProviderId)
        await _db.SaveChangesAsync(ct);

        return user.UserId;
    }
}
