using System.ComponentModel.DataAnnotations;
using LivestockMarket.Models;

namespace LivestockMarket.ViewModels
{
    public class ReportFormVM
    {
        public int ListingId { get; set; }
        public string ListingTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "اختر سبب البلاغ")]
        [Display(Name = "سبب البلاغ")]
        public ReportReason? Reason { get; set; }

        [StringLength(500)]
        [Display(Name = "تفاصيل (اختياري)")]
        public string? Details { get; set; }
    }

    public class ReportRowVM
    {
        public int Id { get; set; }
        public int ListingId { get; set; }
        public string ListingTitle { get; set; } = string.Empty;
        public ListingStatus ListingStatus { get; set; }
        public string SellerId { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public bool SellerBlocked { get; set; }
        public string ReporterName { get; set; } = string.Empty;
        public ReportReason Reason { get; set; }
        public string? Details { get; set; }
        public ReportStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserRowVM
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsAdmin { get; set; }
        public int ListingsCount { get; set; }
        public int ReportsAgainst { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public static class ReportText
    {
        public static string Reason(ReportReason r) => r switch
        {
            ReportReason.Fraud => "احتيال / نصب",
            ReportReason.WrongInfo => "بيانات أو صور غير حقيقية",
            ReportReason.Duplicate => "إعلان مكرر",
            ReportReason.Inappropriate => "محتوى غير لائق",
            _ => "سبب آخر"
        };
    }

    public class ConversationReportFormVM
    {
        public int ConversationId { get; set; }
        public string OtherName { get; set; } = string.Empty;
        public string ListingTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "اختر سبب البلاغ")]
        [Display(Name = "سبب البلاغ")]
        public ReportReason? Reason { get; set; }

        [StringLength(500)]
        [Display(Name = "تفاصيل (اختياري)")]
        public string? Details { get; set; }
    }

    public class ChatReportRowVM
    {
        public int Id { get; set; }
        public string ListingTitle { get; set; } = string.Empty;
        public string ReporterName { get; set; } = string.Empty;
        public string ReportedName { get; set; } = string.Empty;
        public string ReportedUserId { get; set; } = string.Empty;
        public bool ReportedBlocked { get; set; }
        public ReportReason Reason { get; set; }
        public string? Details { get; set; }
        public string Snapshot { get; set; } = string.Empty;
        public ReportStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
