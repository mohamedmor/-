using LivestockMarket.Models;

namespace LivestockMarket.ViewModels
{
    public static class ListingCardExtensions
    {
        public static IQueryable<ListingCardVM> ToCards(this IQueryable<Listing> q) => q.Select(l => new ListingCardVM
        {
            Id = l.Id,
            Title = l.Title,
            MainImage = l.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.SortOrder).Select(i => i.FilePath).FirstOrDefault(),
            CategoryName = l.Category!.Name,
            ConditionName = l.Condition != null ? l.Condition.Name : null,
            Price = l.Price,
            PricingType = l.PricingType,
            Governorate = l.Governorate,
            IsFeatured = l.IsFeatured,
            Quantity = l.Quantity
        });
    }

    public class MarketFilterVM
    {
        public string? Q { get; set; }
        public int? CategoryId { get; set; }
        public int? ConditionId { get; set; }
        public string? Governorate { get; set; }
        public PricingType? PricingType { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public string? Sort { get; set; }      // newest | priceAsc | priceDesc
        public int Page { get; set; } = 1;
    }

    public class MarketVM
    {
        public MarketFilterVM Filter { get; set; } = new();
        public List<ListingCardVM> Items { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<AnimalCondition> Conditions { get; set; } = new();
        public int Total { get; set; }
        public PagerVM Pager { get; set; } = new();
    }

    public class AdminRowVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? MainImage { get; set; }
        public decimal Price { get; set; }
        public PricingType PricingType { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public ListingStatus Status { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminIndexVM
    {
        public ListingStatus Status { get; set; }
        public Dictionary<ListingStatus, int> Counts { get; set; } = new();
        public List<AdminRowVM> Rows { get; set; } = new();
        public PagerVM Pager { get; set; } = new();
    }
}

namespace LivestockMarket.ViewModels
{
    public class SellerProfileVM
    {
        public string SellerId { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int[] Distribution { get; set; } = new int[6];   // index 1..5 = how many reviews with that many stars
        public List<ReviewItemVM> RecentReviews { get; set; } = new();
        // login | self | need-chat | rate | edit
        public string ReviewAction { get; set; } = "login";
        public string Name { get; set; } = string.Empty;
        public string? Governorate { get; set; }
        public string? ProfileImagePath { get; set; }
        public string? Bio { get; set; }
        public DateTime MemberSince { get; set; }
        public int PublishedCount { get; set; }
        public int SoldCount { get; set; }
        public List<ListingCardVM> Items { get; set; } = new();
        public PagerVM Pager { get; set; } = new();
    }
}
