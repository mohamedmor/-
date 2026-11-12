using LivestockMarket.Data;
using LivestockMarket.Models;
using LivestockMarket.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LivestockMarket.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        public HomeController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;

            // only reviewed (Published) listings are ever shown to the public
            IQueryable<ListingCardVM> Cards(IQueryable<Listing> q) => q.Select(l => new ListingCardVM
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

            var published = _db.Listings.AsNoTracking().Where(l => l.Status == ListingStatus.Published);

            var vm = new HomeVM
            {
                Categories = await _db.Categories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync(),
                Featured = await Cards(published.Where(l => l.IsFeatured && (l.FeaturedUntil == null || l.FeaturedUntil > now))
                                                .OrderByDescending(l => l.PublishedAt).Take(8)).ToListAsync(),
                Latest = await Cards(published.OrderByDescending(l => l.PublishedAt).Take(12)).ToListAsync()
            };

            return View(vm);
        }

        public IActionResult Error() => View();
    }
}
