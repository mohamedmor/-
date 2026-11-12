using System.ComponentModel.DataAnnotations;

namespace LivestockMarket.Models
{
    public enum ReportReason { Fraud = 1, WrongInfo = 2, Duplicate = 3, Inappropriate = 4, Other = 5 }
    public enum ReportStatus { Open = 1, Resolved = 2, Dismissed = 3 }

    // A user complaint about a listing (fraud, wrong info, ...)
    public class Report
    {
        public int Id { get; set; }

        public int ListingId { get; set; }
        public Listing? Listing { get; set; }

        [Required]
        public string ReporterId { get; set; } = string.Empty;
        public ApplicationUser? Reporter { get; set; }

        public ReportReason Reason { get; set; }

        [StringLength(500)]
        public string? Details { get; set; }

        public ReportStatus Status { get; set; } = ReportStatus.Open;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? HandledAt { get; set; }
    }
}

namespace LivestockMarket.Models
{
    // A complaint about a chat. It keeps a copy of the last messages as evidence,
    // so it survives even if the listing/conversation is deleted later.
    public class ConversationReport
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }   // plain number on purpose (no FK): the evidence must outlive the chat
        public int? ListingId { get; set; }

        [StringLength(120)]
        public string ListingTitle { get; set; } = string.Empty;

        [Required] public string ReporterId { get; set; } = string.Empty;
        public ApplicationUser? Reporter { get; set; }

        [Required] public string ReportedUserId { get; set; } = string.Empty;
        public ApplicationUser? ReportedUser { get; set; }

        public ReportReason Reason { get; set; }

        [StringLength(500)]
        public string? Details { get; set; }

        public string Snapshot { get; set; } = string.Empty;

        public ReportStatus Status { get; set; } = ReportStatus.Open;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? HandledAt { get; set; }
    }
}
