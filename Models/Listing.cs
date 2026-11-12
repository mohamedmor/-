using System.ComponentModel.DataAnnotations;

namespace LivestockMarket.Models
{
    // One animal offered for sale by one seller
    public class Listing
    {
        public int Id { get; set; }

        [Required]
        public string SellerId { get; set; } = string.Empty;
        public ApplicationUser? Seller { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int? ConditionId { get; set; }
        public AnimalCondition? Condition { get; set; }

        [Required, StringLength(120)]
        public string Title { get; set; } = string.Empty;

        [StringLength(3000)]
        public string? Description { get; set; }

        public PricingType PricingType { get; set; } = PricingType.Fixed;

        // total price when Fixed, price per kilo when PerKg
        public decimal Price { get; set; }

        public decimal? WeightKg { get; set; }

        // how many animals this ad covers (same kind, same price each). 0 = all sold.
        public int Quantity { get; set; } = 1;
        public int? AgeMonths { get; set; }
        public Gender? Gender { get; set; }

        [Required, StringLength(60)]
        public string Governorate { get; set; } = string.Empty;

        [StringLength(80)]
        public string? City { get; set; }

        public ListingStatus Status { get; set; } = ListingStatus.PendingReview;

        [StringLength(300)]
        public string? RejectionReason { get; set; }

        // paid/admin-granted promotion: shows at the top of the lists
        public bool IsFeatured { get; set; }
        public DateTime? FeaturedUntil { get; set; }

        public int ViewsCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PublishedAt { get; set; }

        public List<ListingImage> Images { get; set; } = new();
        public List<ListingAttribute> Attributes { get; set; } = new();
    }

    public class ListingImage
    {
        public int Id { get; set; }
        public int ListingId { get; set; }
        public Listing? Listing { get; set; }

        [Required, StringLength(260)]
        public string FilePath { get; set; } = string.Empty;

        public bool IsMain { get; set; }
        public int SortOrder { get; set; }
    }

    // Free-form details the seller decides: health, milk per day, calf condition, vaccinations ...
    public class ListingAttribute
    {
        public int Id { get; set; }
        public int ListingId { get; set; }
        public Listing? Listing { get; set; }

        [Required, StringLength(60)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Value { get; set; } = string.Empty;
    }
}
