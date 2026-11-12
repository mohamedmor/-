using Microsoft.AspNetCore.Identity;

namespace LivestockMarket.Models
{
    // PhoneNumber, Email, UserName come from IdentityUser
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? Governorate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsBlocked { get; set; }

        // web path like /uploads/avatars/abc.jpg (null = no photo)
        public string? ProfileImagePath { get; set; }

        public string? Bio { get; set; }
    }
}
