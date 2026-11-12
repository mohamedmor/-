using LivestockMarket.Data;
using LivestockMarket.Models;
using LivestockMarket.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LivestockMarket.Controllers
{
    // Public seller page: name, area, rating, reviews and his published listings. No phone/email here.
    public class SellersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public SellersController(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        public async Task<IActionResult> Profile(string id, int page = 1)
        {
            var seller = await _users.FindByIdAsync(id);
            if (seller == null || seller.IsBlocked) return NotFound();

            var me = _users.GetUserId(User);
            var mine = _db.Listings.AsNoTracking().Where(l => l.SellerId == id);

            const int size = 12;
            var publishedCount = await mine.CountAsync(l => l.Status == ListingStatus.Published);
            page = PagerVM.Clamp(page, publishedCount, size);

            // ---- rating summary ----
            var reviews = _db.SellerReviews.AsNoTracking().Where(r => r.SellerId == id && !r.IsHidden);
            var byStars = await reviews.GroupBy(r => r.Rating).Select(g => new { Stars = g.Key, N = g.Count() }).ToListAsync();
            var dist = new int[6];
            foreach (var g in byStars) if (g.Stars is >= 1 and <= 5) dist[g.Stars] = g.N;
            var reviewCount = dist.Sum();
            var average = reviewCount == 0 ? 0 : Enumerable.Range(1, 5).Sum(s => s * dist[s]) / (double)reviewCount;

            var recent = await reviews.OrderByDescending(r => r.CreatedAt).Take(5)
                .Select(r => new ReviewItemVM
                {
                    Id = r.Id,
                    SellerId = r.SellerId,
                    ReviewerName = r.Reviewer!.FullName,
                    ReviewerAvatar = r.Reviewer.ProfileImagePath,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    IsMine = me != null && r.ReviewerId == me
                }).ToListAsync();

            // what should the "rate this seller" button do for the visitor?
            string action;
            if (me == null) action = "login";
            else if (me == id) action = "self";
            else if (await _db.SellerReviews.AnyAsync(r => r.SellerId == id && r.ReviewerId == me)) action = "edit";
            else if (await _db.Conversations.AnyAsync(c => c.BuyerId == me && c.SellerId == id)) action = "rate";
            else action = "need-chat";

            var vm = new SellerProfileVM
            {
                SellerId = id,
                Name = seller.FullName,
                Governorate = seller.Governorate,
                ProfileImagePath = seller.ProfileImagePath,
                Bio = seller.Bio,
                MemberSince = seller.CreatedAt,
                AverageRating = average,
                ReviewCount = reviewCount,
                Distribution = dist,
                RecentReviews = recent,
                ReviewAction = action,
                PublishedCount = publishedCount,
                SoldCount = await mine.CountAsync(l => l.Status == ListingStatus.Sold),
                Items = await mine.Where(l => l.Status == ListingStatus.Published)
                                  .OrderByDescending(l => l.PublishedAt).Skip((page - 1) * size).Take(size).ToCards().ToListAsync(),
                Pager = new PagerVM
                {
                    Page = page, PageSize = size, Total = publishedCount, Controller = "Sellers", Action = "Profile",
                    Route = new Dictionary<string, string?> { ["id"] = id }
                }
            };
            return View(vm);
        }

        // every review of one seller, paged
        public async Task<IActionResult> Reviews(string id, int page = 1)
        {
            var seller = await _users.FindByIdAsync(id);
            if (seller == null || seller.IsBlocked) return NotFound();

            var me = _users.GetUserId(User);
            const int size = 10;
            var q = _db.SellerReviews.AsNoTracking().Where(r => r.SellerId == id && !r.IsHidden);
            var total = await q.CountAsync();
            page = PagerVM.Clamp(page, total, size);
            var avg = total == 0 ? 0 : await q.AverageAsync(r => (double)r.Rating);

            var items = await q.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * size).Take(size)
                .Select(r => new ReviewItemVM
                {
                    Id = r.Id,
                    SellerId = r.SellerId,
                    ReviewerName = r.Reviewer!.FullName,
                    ReviewerAvatar = r.Reviewer.ProfileImagePath,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    IsMine = me != null && r.ReviewerId == me
                }).ToListAsync();

            return View(new ReviewsPageVM
            {
                SellerId = id,
                SellerName = seller.FullName,
                Average = avg,
                Count = total,
                Items = items,
                Pager = new PagerVM
                {
                    Page = page, PageSize = size, Total = total, Controller = "Sellers", Action = "Reviews",
                    Route = new Dictionary<string, string?> { ["id"] = id }
                }
            });
        }
    }
}
