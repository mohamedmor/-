using LivestockMarket.Data;
using LivestockMarket.Models;
using LivestockMarket.Utilities;
using LivestockMarket.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LivestockMarket.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class ModerationController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public ModerationController(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        // ---------- Reports ----------
        public async Task<IActionResult> Reports(ReportStatus status = ReportStatus.Open)
        {
            ViewData["Status"] = status;
            var rows = await _db.Reports.AsNoTracking()
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.CreatedAt)
                .Take(200)
                .Select(r => new ReportRowVM
                {
                    Id = r.Id,
                    ListingId = r.ListingId,
                    ListingTitle = r.Listing!.Title,
                    ListingStatus = r.Listing!.Status,
                    SellerId = r.Listing.SellerId,
                    SellerName = r.Listing.Seller!.FullName,
                    SellerBlocked = r.Listing.Seller!.IsBlocked,
                    ReporterName = r.Reporter!.FullName,
                    Reason = r.Reason,
                    Details = r.Details,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                }).ToListAsync();
            return View(rows);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Dismiss(int id)
        {
            var r = await _db.Reports.FindAsync(id);
            if (r == null) return NotFound();
            r.Status = ReportStatus.Dismissed;
            r.HandledAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Reports));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id)
        {
            var r = await _db.Reports.FindAsync(id);
            if (r == null) return NotFound();
            r.Status = ReportStatus.Resolved;
            r.HandledAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Reports));
        }

        // ---------- Chat reports ----------
        public async Task<IActionResult> ChatReports(ReportStatus status = ReportStatus.Open)
        {
            ViewData["Status"] = status;
            var rows = await _db.ConversationReports.AsNoTracking()
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.CreatedAt).Take(200)
                .Select(r => new ChatReportRowVM
                {
                    Id = r.Id,
                    ListingTitle = r.ListingTitle,
                    ReporterName = r.Reporter!.FullName,
                    ReportedName = r.ReportedUser!.FullName,
                    ReportedUserId = r.ReportedUserId,
                    ReportedBlocked = r.ReportedUser.IsBlocked,
                    Reason = r.Reason,
                    Details = r.Details,
                    Snapshot = r.Snapshot,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                }).ToListAsync();
            return View(rows);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseChatReport(int id, bool dismiss)
        {
            var r = await _db.ConversationReports.FindAsync(id);
            if (r == null) return NotFound();
            r.Status = dismiss ? ReportStatus.Dismissed : ReportStatus.Resolved;
            r.HandledAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(ChatReports));
        }

        // ---------- Seller reviews ----------
        public async Task<IActionResult> Reviews(bool hiddenOnly = false, int page = 1)
        {
            const int size = 20;
            var q = _db.SellerReviews.AsNoTracking().AsQueryable();
            if (hiddenOnly) q = q.Where(r => r.IsHidden);
            var total = await q.CountAsync();
            page = PagerVM.Clamp(page, total, size);

            var rows = await q.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * size).Take(size)
                .Select(r => new AdminReviewRowVM
                {
                    Id = r.Id,
                    SellerId = r.SellerId,
                    SellerName = r.Seller!.FullName,
                    ReviewerName = r.Reviewer!.FullName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    IsHidden = r.IsHidden,
                    CreatedAt = r.CreatedAt
                }).ToListAsync();

            ViewData["HiddenOnly"] = hiddenOnly;
            ViewData["Pager"] = new PagerVM
            {
                Page = page, PageSize = size, Total = total, Controller = "Moderation", Action = "Reviews",
                Route = new Dictionary<string, string?> { ["hiddenOnly"] = hiddenOnly ? "true" : null }
            };
            return View(rows);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleReviewHidden(int id)
        {
            var r = await _db.SellerReviews.FindAsync(id);
            if (r == null) return NotFound();
            r.IsHidden = !r.IsHidden;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Reviews));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var r = await _db.SellerReviews.FindAsync(id);
            if (r == null) return NotFound();
            _db.SellerReviews.Remove(r);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Reviews));
        }

        // ---------- App feedback ----------
        public async Task<IActionResult> AppFeedback(int page = 1)
        {
            const int size = 20;
            var all = _db.AppFeedbacks.AsNoTracking();
            var total = await all.CountAsync();
            page = PagerVM.Clamp(page, total, size);
            var avg = total == 0 ? 0 : await all.AverageAsync(f => (double)f.Rating);

            var rows = await all.OrderBy(f => f.IsRead).ThenByDescending(f => f.CreatedAt)
                .Skip((page - 1) * size).Take(size)
                .Select(f => new AdminFeedbackRowVM
                {
                    Id = f.Id,
                    UserName = f.User!.FullName,
                    Rating = f.Rating,
                    Kind = f.Kind,
                    Message = f.Message,
                    IsRead = f.IsRead,
                    CreatedAt = f.CreatedAt
                }).ToListAsync();

            return View(new AdminFeedbackVM
            {
                Average = avg,
                Total = total,
                Items = rows,
                Pager = new PagerVM { Page = page, PageSize = size, Total = total, Controller = "Moderation", Action = "AppFeedback" }
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFeedbackRead(int id)
        {
            var f = await _db.AppFeedbacks.FindAsync(id);
            if (f == null) return NotFound();
            f.IsRead = true;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(AppFeedback));
        }

        // ---------- Contact messages ----------
        public async Task<IActionResult> Contacts(bool handled = false, int page = 1)
        {
            const int size = 20;
            var q = _db.ContactMessages.AsNoTracking().Where(m => m.IsHandled == handled);
            var total = await q.CountAsync();
            page = PagerVM.Clamp(page, total, size);

            var rows = await q.OrderByDescending(m => m.CreatedAt).Skip((page - 1) * size).Take(size)
                .Select(m => new AdminContactRowVM
                {
                    Id = m.Id,
                    UserName = m.User!.FullName,
                    Email = m.User.Email,
                    Phone = m.Phone ?? m.User.PhoneNumber,
                    Subject = m.Subject,
                    Message = m.Message,
                    IsHandled = m.IsHandled,
                    CreatedAt = m.CreatedAt
                }).ToListAsync();

            ViewData["Handled"] = handled;
            ViewData["Pager"] = new PagerVM
            {
                Page = page, PageSize = size, Total = total, Controller = "Moderation", Action = "Contacts",
                Route = new Dictionary<string, string?> { ["handled"] = handled ? "true" : "false" }
            };
            return View(rows);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkContactHandled(int id)
        {
            var m = await _db.ContactMessages.FindAsync(id);
            if (m == null) return NotFound();
            m.IsHandled = true;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Contacts));
        }

        // ---------- Users ----------
        public async Task<IActionResult> Users(string? q)
        {
            ViewData["Q"] = q;
            var query = _db.Users.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim();
                query = query.Where(u => u.FullName.Contains(t) || (u.Email != null && u.Email.Contains(t))
                                         || (u.PhoneNumber != null && u.PhoneNumber.Contains(t)));
            }

            var adminIds = (await _users.GetUsersInRoleAsync(Roles.Admin)).Select(u => u.Id).ToHashSet();

            var rows = await query.OrderByDescending(u => u.CreatedAt).Take(100)
                .Select(u => new UserRowVM
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    IsBlocked = u.IsBlocked,
                    CreatedAt = u.CreatedAt,
                    ListingsCount = _db.Listings.Count(l => l.SellerId == u.Id),
                    ReportsAgainst = _db.Reports.Count(r => r.Listing!.SellerId == u.Id)
                }).ToListAsync();
            foreach (var r in rows) r.IsAdmin = adminIds.Contains(r.Id);

            return View(rows);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.Id == _users.GetUserId(User) || await _users.IsInRoleAsync(user, Roles.Admin))
            {
                TempData["Error"] = "مينفعش تحظر مدير.";
                return RedirectToAction(nameof(Users));
            }

            user.IsBlocked = !user.IsBlocked;
            await _users.UpdateAsync(user);
            // changing the stamp kills his current login cookie (validated on every request, see Program.cs)
            await _users.UpdateSecurityStampAsync(user);

            if (user.IsBlocked)
            {
                // take everything he has live or waiting off the market
                var live = await _db.Listings
                    .Where(l => l.SellerId == user.Id &&
                                (l.Status == ListingStatus.Published || l.Status == ListingStatus.PendingReview || l.Status == ListingStatus.Reserved))
                    .ToListAsync();
                foreach (var l in live)
                {
                    l.Status = ListingStatus.Rejected;
                    l.RejectionReason = "تم إيقاف الإعلان بسبب إيقاف الحساب";
                    l.PublishedAt = null;
                    l.IsFeatured = false;
                }
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = user.IsBlocked ? "تم حظر المستخدم وإيقاف إعلاناته." : "تم فك الحظر. إعلاناته الموقوفة تظهر عنده \"مرفوض\" وممكن يعدّلها ويبعتها للمراجعة تاني.";
            return RedirectToAction(nameof(Users));
        }
    }
}
