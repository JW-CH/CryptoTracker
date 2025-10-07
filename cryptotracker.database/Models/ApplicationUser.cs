using Microsoft.AspNetCore.Identity;

namespace cryptotracker.database.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? DisplayName { get; set; }
    }
}
