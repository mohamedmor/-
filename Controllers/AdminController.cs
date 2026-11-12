using LivestockMarket.Data;
using LivestockMarket.Models;
using LivestockMarket.Services;
using LivestockMarket.Utilities;
using LivestockMarket.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LivestockMarket.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        public AdminController(ApplicationDbContext db) => _db = db;

        // list by status (default: waiting for review)
        public async Task<IActionResult> Index(ListingStatus status = ListingStatus.PendingReview, int page = 1)
        {
            var counts = await _db.Listings.AsNoTracking().GroupBy(l => l.Status)
                .Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);

            ViewData["NewContacts"] = await _db.ContactMessages.CountAsync(m => !m.IsHandled);
            ViewData["NewFeedback"] = await _db.AppFeedbacks.CountAsync(f => !f.IsRead);

            const int size = 20;
            var total = counts.TryGetValue(status, out var n) ? n : 0;
            page = PagerVM.Clamp(page, total, size);

            var rows = await _db.Listings.AsNoTracking()
                .Where(l => l.Status == status)
                .OrderBy(l => l.CreatedAt)
                .Skip((page - 1) * size).Take(size)
                .Select(l => new AdminRowVM
                {
                    Id = l.Id,
                    Title = l.Title,
                    Price = l.Price,
                    PricingType = l.PricingType,
                    CategoryName = l.Category!.Name,
                    SellerName = l.Seller!.FullName,
                    Status = l.Status,
                    IsFeatured = l.IsFeatured,
                    CreatedAt = l.CreatedAt,
                    MainImage = l.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.SortOrder).Select(i => i.FilePath).FirstOrDefault()
                }).ToListAsync();

            return View(new AdminIndexVM
            {
                Status = status, Counts = counts, Rows = rows,
                Pager = new PagerVM
                {
                    Page = page, PageSize = size, Total = total, Controller = "Admin", Action = "Index",
                    Route = new Dictionary<string, string?> { ["status"] = status.ToString() }
                }
            });
        }

        public async Task<IActionResult> Review(int id)
        {
            var l = await _db.Listings.AsNoTracking()
                .Include(x => x.Category).Include(x => x.Condition)
                .Include(x => x.Images).Include(x => x.Attributes).Include(x => x.Seller)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (l == null) return NotFound();
            return View(l);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var l = await _db.Listings.FindAsync(id);
            if (l == null) return NotFound();
            l.Status = ListingStatus.Published;
            l.PublishedAt = DateTime.UtcNow;
            l.RejectionReason = null;
            await _db.SaveChangesAsync();
            TempData["Success"] = "تم نشر الإعلان.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reason)
        {
            var l = await _db.Listings.FindAsync(id);
            if (l == null) return NotFound();
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "اكتب سبب الرفض عشان البائع يعرف يعدّل.";
                return RedirectToAction(nameof(Review), new { id });
            }
            l.Status = ListingStatus.Rejected;
            l.PublishedAt = null;
            l.IsFeatured = false;
            l.RejectionReason = reason.Trim().Length > 300 ? reason.Trim()[..300] : reason.Trim();
            await _db.SaveChangesAsync();
            TempData["Success"] = "تم رفض الإعلان.";
            return RedirectToAction(nameof(Index));
        }

        // days = 0 removes the feature; otherwise features the listing for that many days
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Feature(int id, int days)
        {
            var l = await _db.Listings.FindAsync(id);
            if (l == null) return NotFound();
            days = Math.Clamp(days, 0, 365);
            l.IsFeatured = days > 0;
            l.FeaturedUntil = days > 0 ? DateTime.UtcNow.AddDays(days) : null;
            await _db.SaveChangesAsync();
            TempData["Success"] = days > 0 ? $"الإعلان مميز لمدة {days} يوم." : "تم إلغاء التمييز.";
            return RedirectToAction(nameof(Review), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, [FromServices] ImageStorage images)
        {
            var l = await _db.Listings.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
            if (l == null) return NotFound();
            var paths = l.Images.Select(i => i.FilePath).ToList();
            _db.Listings.Remove(l);
            await _db.SaveChangesAsync();
            foreach (var p in paths) images.Delete(p);
            TempData["Success"] = "تم حذف الإعلان.";
            return RedirectToAction(nameof(Index));
        }
    }
}
