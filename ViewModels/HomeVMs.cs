using LivestockMarket.Models;

namespace LivestockMarket.ViewModels
{
    public class ListingCardVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? MainImage { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? ConditionName { get; set; }
        public decimal Price { get; set; }
        public PricingType PricingType { get; set; }
        public string Governorate { get; set; } = string.Empty;
        public bool IsFeatured { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class HomeVM
    {
        public List<Category> Categories { get; set; } = new();
        public List<ListingCardVM> Featured { get; set; } = new();
        public List<ListingCardVM> Latest { get; set; } = new();
    }
}
