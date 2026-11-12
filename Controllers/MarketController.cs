using LivestockMarket.Data;
using LivestockMarket.Models;
using LivestockMarket.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LivestockMarket.Controllers
{
    // Public browsing with filters. Only Published listings are ever returned.
    public class MarketController : Controller
    {
        private const int PageSize = 20;
        private readonly ApplicationDbContext _db;
        public MarketController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(MarketFilterVM f)
        {
            if (f.Page < 1) f.Page = 1;
            var now = DateTime.UtcNow;

            var q = _db.Listings.AsNoTracking().Where(l => l.Status == ListingStatus.Published);

            if (!string.IsNullOrWhiteSpace(f.Q))
            {
                var term = f.Q.Trim();
                q = q.Where(l => l.Title.Contains(term) || (l.Description != null && l.Description.Contains(term)));
            }
            if (f.CategoryId != null) q = q.Where(l => l.CategoryId == f.CategoryId);
            if (f.ConditionId != null) q = q.Where(l => l.ConditionId == f.ConditionId);
            if (!string.IsNullOrWhiteSpace(f.Governorate)) q = q.Where(l => l.Governorate == f.Governorate);
            if (f.PricingType != null) q = q.Where(l => l.PricingType == f.PricingType);
            if (f.MinPrice != null) q = q.Where(l => l.Price >= f.MinPrice);
            if (f.MaxPrice != null) q = q.Where(l => l.Price <= f.MaxPrice);
            if (f.MinAge != null) q = q.Where(l => l.AgeMonths != null && l.AgeMonths >= f.MinAge);
            if (f.MaxAge != null) q = q.Where(l => l.AgeMonths != null && l.AgeMonths <= f.MaxAge);

            var total = await q.CountAsync();
            f.Page = PagerVM.Clamp(f.Page, total, PageSize);   // a page number past the end falls back to the last page

            // featured (still valid) first, then the chosen sort
            var ordered = q.OrderByDescending(l => l.IsFeatured && (l.FeaturedUntil == null || l.FeaturedUntil > now));
            ordered = f.Sort switch
            {
                "priceAsc" => ordered.ThenBy(l => l.Price),
                "priceDesc" => ordered.ThenByDescending(l => l.Price),
                _ => ordered.ThenByDescending(l => l.PublishedAt)
            };

            var items = await ordered.Skip((f.Page - 1) * PageSize).Take(PageSize).ToCards().ToListAsync();

            return View(new MarketVM
            {
                Filter = f,
                Items = items,
                Pager = new PagerVM
                {
                    Page = f.Page, PageSize = PageSize, Total = total, Controller = "Market", Action = "Index",
                    Route = new Dictionary<string, string?>
                    {
                        ["Q"] = f.Q, ["CategoryId"] = f.CategoryId?.ToString(), ["ConditionId"] = f.ConditionId?.ToString(),
                        ["Governorate"] = f.Governorate, ["PricingType"] = f.PricingType == null ? null : ((int)f.PricingType).ToString(),
                        ["MinPrice"] = f.MinPrice?.ToString(), ["MaxPrice"] = f.MaxPrice?.ToString(),
                        ["MinAge"] = f.MinAge?.ToString(), ["MaxAge"] = f.MaxAge?.ToString(), ["Sort"] = f.Sort
                    }
                },
                Total = total,
                Categories = await _db.Categories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync(),
                Conditions = await _db.AnimalConditions.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync()
            });
        }
    }
}
