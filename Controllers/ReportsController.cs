using LivestockMarket.Data;
using LivestockMarket.Models;
using LivestockMarket.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LivestockMarket.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private const int MaxReportsPerDay = 10;
        private const int AutoHideThreshold = 3;   // distinct open reports that pull a listing back to review

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public ReportsController(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int listingId)
        {
            var l = await FindReportableAsync(listingId);
            if (l == null) return NotFound();
            return View(new ReportFormVM { ListingId = l.Id, ListingTitle = l.Title });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReportFormVM vm)
        {
            var l = await FindReportableAsync(vm.ListingId);
            if (l == null) return NotFound();
            vm.ListingTitle = l.Title;

            var uid = _users.GetUserId(User)!;

            if (await _db.Reports.AnyAsync(r => r.ListingId == l.Id && r.ReporterId == uid))
                ModelState.AddModelError("", "إنت بلّغت عن الإعلان ده قبل كده.");

            var since = DateTime.UtcNow.AddDays(-1);
            if (await _db.Reports.CountAsync(r => r.ReporterId == uid && r.CreatedAt > since) >= MaxReportsPerDay)
                ModelState.AddModelError("", "وصلت للحد الأقصى من البلاغات النهاردة.");

            if (!ModelState.IsValid) return View(vm);

            _db.Reports.Add(new Report
            {
                ListingId = l.Id,
                ReporterId = uid,
                Reason = vm.Reason!.Value,
                Details = string.IsNullOrWhiteSpace(vm.Details) ? null : vm.Details.Trim()
            });
            await _db.SaveChangesAsync();

            // several different people reporting the same ad: take it off the public list until the Admin looks at it
            var open = await _db.Reports.CountAsync(r => r.ListingId == l.Id && r.Status == ReportStatus.Open);
            if (open >= AutoHideThreshold && l.Status == ListingStatus.Published)
            {
                var tracked = await _db.Listings.FindAsync(l.Id);
                if (tracked != null)
                {
                    tracked.Status = ListingStatus.PendingReview;
                    tracked.PublishedAt = null;
                    await _db.SaveChangesAsync();
                }
            }

            TempData["Success"] = "شكراً، البلاغ وصل للإدارة وهيتراجع.";
            return RedirectToAction("Details", "Listings", new { id = l.Id });
        }

        // ---------- report a chat ----------
        [HttpGet]
        public async Task<IActionResult> Chat(int id)
        {
            var uid = _users.GetUserId(User)!;
            var c = await _db.Conversations.AsNoTracking().Include(x => x.Listing).Include(x => x.Buyer).Include(x => x.Seller)
                .FirstOrDefaultAsync(x => x.Id == id && (x.BuyerId == uid || x.SellerId == uid));
            if (c == null) return NotFound();
            return View(new ConversationReportFormVM
            {
                ConversationId = id,
                ListingTitle = c.Listing!.Title,
                OtherName = (c.BuyerId == uid ? c.Seller! : c.Buyer!).FullName
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Chat(ConversationReportFormVM vm)
        {
            var uid = _users.GetUserId(User)!;
            var c = await _db.Conversations.AsNoTracking().Include(x => x.Listing).Include(x => x.Buyer).Include(x => x.Seller)
                .FirstOrDefaultAsync(x => x.Id == vm.ConversationId && (x.BuyerId == uid || x.SellerId == uid));
            if (c == null) return NotFound();

            var other = c.BuyerId == uid ? c.Seller! : c.Buyer!;
            vm.OtherName = other.FullName;
            vm.ListingTitle = c.Listing!.Title;

            if (await _db.ConversationReports.AnyAsync(r => r.ConversationId == c.Id && r.ReporterId == uid))
                ModelState.AddModelError("", "إنت بلّغت عن المحادثة دي قبل كده.");

            var since = DateTime.UtcNow.AddDays(-1);
            if (await _db.ConversationReports.CountAsync(r => r.ReporterId == uid && r.CreatedAt > since) >= MaxReportsPerDay)
                ModelState.AddModelError("", "وصلت للحد الأقصى من البلاغات النهاردة.");

            if (!ModelState.IsValid) return View(vm);

            // evidence: the last 30 messages, both sides
            var last = await _db.Messages.AsNoTracking().Where(m => m.ConversationId == c.Id)
                .OrderByDescending(m => m.Id).Take(30).ToListAsync();
            last.Reverse();
            var snapshot = string.Join("\n", last.Select(m =>
                $"[{m.SentAt:yyyy-MM-dd HH:mm} UTC] {(m.SenderId == uid ? "المبلّغ" : other.FullName)}: {m.Body}"));
            if (snapshot.Length > 7900) snapshot = snapshot[^7900..];

            _db.ConversationReports.Add(new ConversationReport
            {
                ConversationId = c.Id,
                ListingId = c.ListingId,
                ListingTitle = c.Listing.Title.Length > 120 ? c.Listing.Title[..120] : c.Listing.Title,
                ReporterId = uid,
                ReportedUserId = other.Id,
                Reason = vm.Reason!.Value,
                Details = string.IsNullOrWhiteSpace(vm.Details) ? null : vm.Details.Trim(),
                Snapshot = snapshot
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = "شكراً، بلاغك وصل للإدارة مع آخر الرسايل كدليل.";
            return RedirectToAction("Thread", "Messages", new { id = c.Id });
        }

        // only published ads that are not the user's own can be reported
        private async Task<Listing?> FindReportableAsync(int id)
        {
            var uid = _users.GetUserId(User);
            return await _db.Listings.AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == id && l.Status == ListingStatus.Published && l.SellerId != uid);
        }
    }
}
