using System.ComponentModel.DataAnnotations;

namespace LivestockMarket.Models
{
    // A customer's rating + comment about a seller (one per reviewer per seller, editable)
    public class SellerReview
    {
        public int Id { get; set; }

        [Required] public string SellerId { get; set; } = string.Empty;
        public ApplicationUser? Seller { get; set; }

        [Required] public string ReviewerId { get; set; } = string.Empty;
        public ApplicationUser? Reviewer { get; set; }

        public int Rating { get; set; }   // 1..5

        [StringLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // the Admin can hide an abusive review without deleting it
        public bool IsHidden { get; set; }
    }

    public enum FeedbackKind { Praise = 1, Suggestion = 2, Problem = 3 }

    // What a user thinks of the site itself
    public class AppFeedback
    {
        public int Id { get; set; }

        [Required] public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public int Rating { get; set; }   // 1..5
        public FeedbackKind Kind { get; set; } = FeedbackKind.Suggestion;

        [Required, StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }
    }

    // "Contact the administration" messages
    public class ContactMessage
    {
        public int Id { get; set; }

        [Required] public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required, StringLength(120)]
        public string Subject { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }   // optional: a number to call back on

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsHandled { get; set; }
    }
}
