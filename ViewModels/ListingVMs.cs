using System.ComponentModel.DataAnnotations;
using LivestockMarket.Models;

namespace LivestockMarket.ViewModels
{
    public class AttributeVM
    {
        [StringLength(60)] public string? Name { get; set; }
        [StringLength(200)] public string? Value { get; set; }
    }

    public class ListingFormVM
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "اكتب عنوان الإعلان"), StringLength(120)]
        [Display(Name = "عنوان الإعلان")]
        public string Title { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "اختر القسم")]
        [Display(Name = "القسم")]
        public int CategoryId { get; set; }

        [Display(Name = "الحالة")]
        public int? ConditionId { get; set; }

        [Display(Name = "طريقة التسعير")]
        public PricingType PricingType { get; set; } = PricingType.Fixed;

        [Range(1, 100000000, ErrorMessage = "اكتب سعر صحيح")]
        [Display(Name = "السعر")]
        public decimal Price { get; set; }

        [Range(1, 5000, ErrorMessage = "الوزن غير منطقي")]
        [Display(Name = "الوزن التقريبي (كجم)")]
        public decimal? WeightKg { get; set; }

        [Range(1, 1000, ErrorMessage = "العدد من 1 لـ 1000")]
        [Display(Name = "العدد المتاح")]
        public int Quantity { get; set; } = 1;

        [Range(0, 360, ErrorMessage = "السن غير منطقي")]
        [Display(Name = "السن (بالشهور)")]
        public int? AgeMonths { get; set; }

        [Display(Name = "النوع")]
        public Gender? Gender { get; set; }

        [Required(ErrorMessage = "اختر المحافظة")]
        [Display(Name = "المحافظة")]
        public string Governorate { get; set; } = string.Empty;

        [StringLength(80)]
        [Display(Name = "المدينة / المركز")]
        public string? City { get; set; }

        [StringLength(3000)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        public List<AttributeVM> Attributes { get; set; } = new();

        public List<IFormFile>? NewImages { get; set; }
        public List<int>? RemoveImageIds { get; set; }

        // for display only (never bound from the form)
        public List<ListingImage> ExistingImages { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<AnimalCondition> Conditions { get; set; } = new();
    }

    public class MyListingVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? MainImage { get; set; }
        public decimal Price { get; set; }
        public PricingType PricingType { get; set; }
        public int Quantity { get; set; } = 1;
        public ListingStatus Status { get; set; }
        public string? RejectionReason { get; set; }
        public int ViewsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MyListingsVM
    {
        public List<MyListingVM> Items { get; set; } = new();
        public PagerVM Pager { get; set; } = new();
    }

    public class ListingDetailsVM
    {
        public Listing Listing { get; set; } = null!;
        public string SellerName { get; set; } = string.Empty;
        public string? SellerPhone { get; set; }   // null when the visitor is not logged in
        public bool IsOwner { get; set; }
        public double SellerRating { get; set; }
        public int SellerReviewCount { get; set; }
    }
}
