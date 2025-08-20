using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace cryptotracker.database.Models
{
    /// <summary>
    /// Application user mapped to a single external OIDC provider.
    /// AuthProviderId stores the provider's stable subject (sub).
    /// </summary>
    [Index(nameof(AuthProviderId), IsUnique = true)]
    [Index(nameof(UserName), IsUnique = true)]
    public class User
    {
        [Key]
        public Guid UserId { get; set; }

        /// <summary>
        /// Stable identifier from the auth provider (OIDC 'sub').
        /// Unique across all users since we only have one provider.
        /// </summary>

        [Required]
        [MaxLength(200)]
        public string AuthProviderId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(320)]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsAdmin { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
    }
}