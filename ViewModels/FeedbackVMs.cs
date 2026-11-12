using System.ComponentModel.DataAnnotations;
using LivestockMarket.Models;

namespace LivestockMarket.ViewModels
{
    public class ReviewFormVM
    {
        public string SellerId { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public bool IsEdit { get; set; }

        [Range(1, 5, ErrorMessage = "اختار عدد النجوم")]
        public int Rating { get; set; }

        [StringLength(500, ErrorMessage = "التعليق 500 حرف بحد أقصى")]
        [Display(Name = "تعليقك (اختياري)")]
        public string? Comment { get; set; }
    }

    public class ReviewItemVM
    {
        public int Id { get; set; }
        public string SellerId { get; set; } = string.Empty;
        public string ReviewerName { get; set; } = string.Empty;
        public string? ReviewerAvatar { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsMine { get; set; }
    }

    public class FeedbackFormVM
    {
        [Range(1, 5, ErrorMessage = "اختار عدد النجوم")]
        public int Rating { get; set; }

        [Display(Name = "نوع الرأي")]
        public FeedbackKind Kind { get; set; } = FeedbackKind.Suggestion;

        [Required(ErrorMessage = "اكتب رأيك"), StringLength(1000, ErrorMessage = "الرأي 1000 حرف بحد أقصى")]
        [Display(Name = "رأيك")]
        public string Message { get; set; } = string.Empty;
    }

    public class ContactFormVM
    {
        [Required(ErrorMessage = "اكتب عنوان الرسالة"), StringLength(120)]
        [Display(Name = "الموضوع")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "اكتب رسالتك"), StringLength(2000, ErrorMessage = "الرسالة 2000 حرف بحد أقصى")]
        [Display(Name = "رسالتك")]
        public string Message { get; set; } = string.Empty;

        [RegularExpression(@"^01[0125][0-9]{8}$", ErrorMessage = "رقم موبايل مصري صحيح (11 رقم يبدأ بـ 01)")]
        [Display(Name = "رقم للتواصل (اختياري)")]
        public string? Phone { get; set; }
    }

    public class ReviewsPageVM
    {
        public string SellerId { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public double Average { get; set; }
        public int Count { get; set; }
        public List<ReviewItemVM> Items { get; set; } = new();
        public PagerVM Pager { get; set; } = new();
    }

    // ---- admin ----
    public class AdminReviewRowVM
    {
        public int Id { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string SellerId { get; set; } = string.Empty;
        public string ReviewerName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsHidden { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminFeedbackVM
    {
        public double Average { get; set; }
        public int Total { get; set; }
        public List<AdminFeedbackRowVM> Items { get; set; } = new();
        public PagerVM Pager { get; set; } = new();
    }

    public class AdminContactRowVM
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsHandled { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminFeedbackRowVM
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public FeedbackKind Kind { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
