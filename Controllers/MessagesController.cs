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
    public class MessagesController : Controller
    {
        private const int MaxBody = 1000;
        private const int MaxPerMinute = 15;

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public MessagesController(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        // ---------- Inbox ----------
        public async Task<IActionResult> Inbox()
        {
            var uid = _users.GetUserId(User)!;
            var rows = await _db.Conversations.AsNoTracking()
                .Where(c => c.BuyerId == uid || c.SellerId == uid)
                .OrderByDescending(c => c.LastMessageAt)
                .Take(100)
                .Select(c => new ConversationRowVM
                {
                    Id = c.Id,
                    ListingTitle = c.Listing!.Title,
                    OtherName = c.BuyerId == uid ? c.Seller!.FullName : c.Buyer!.FullName,
                    OtherAvatar = c.BuyerId == uid ? c.Seller!.ProfileImagePath : c.Buyer!.ProfileImagePath,
                    LastText = c.Messages.OrderByDescending(m => m.Id).Select(m => m.Body).FirstOrDefault(),
                    LastAt = c.LastMessageAt,
                    Unread = c.Messages.Count(m => m.SenderId != uid && m.ReadAt == null)
                }).ToListAsync();
            return View(rows);
        }

        // ---------- Start a chat from a listing (POST: it creates data) ----------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int listingId)
        {
            var me = await _users.GetUserAsync(User);
            if (me == null || me.IsBlocked) return Forbid();

            var listing = await _db.Listings.AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == listingId && l.Status == ListingStatus.Published && l.SellerId != me.Id);
            if (listing == null) return NotFound();

            var conv = await _db.Conversations.FirstOrDefaultAsync(c => c.ListingId == listingId && c.BuyerId == me.Id);
            if (conv == null)
            {
                conv = new Conversation { ListingId = listingId, BuyerId = me.Id, SellerId = listing.SellerId };
                _db.Conversations.Add(conv);
                try { await _db.SaveChangesAsync(); }
                catch (DbUpdateException)
                {
                    // double click: the other request already created it
                    _db.Entry(conv).State = EntityState.Detached;
                    conv = await _db.Conversations.FirstAsync(c => c.ListingId == listingId && c.BuyerId == me.Id);
                }
            }
            return RedirectToAction(nameof(Thread), new { id = conv.Id });
        }

        // ---------- A conversation ----------
        public async Task<IActionResult> Thread(int id)
        {
            var uid = _users.GetUserId(User)!;
            var c = await _db.Conversations.AsNoTracking()
                .Include(x => x.Listing).Include(x => x.Buyer).Include(x => x.Seller)
                .FirstOrDefaultAsync(x => x.Id == id && (x.BuyerId == uid || x.SellerId == uid));
            if (c == null) return NotFound();

            var isBuyer = c.BuyerId == uid;
            var other = isBuyer ? c.Seller! : c.Buyer!;

            var msgs = await _db.Messages.AsNoTracking()
                .Where(m => m.ConversationId == id)
                .OrderByDescending(m => m.Id).Take(200).ToListAsync();
            msgs.Reverse();

            await MarkReadAsync(id, uid);

            return View(new ThreadVM
            {
                ConversationId = id,
                ListingId = c.ListingId,
                ListingTitle = c.Listing!.Title,
                ListingStatus = c.Listing.Status,
                OtherId = other.Id,
                OtherName = other.FullName,
                OtherAvatar = other.ProfileImagePath,
                IsBuyer = isBuyer,
                Messages = msgs.Select(m => ToItem(m, uid)).ToList()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int id, string? body)
        {
            var me = await _users.GetUserAsync(User);
            if (me == null || me.IsBlocked) return Forbid();

            var c = await _db.Conversations.FirstOrDefaultAsync(x => x.Id == id && (x.BuyerId == me.Id || x.SellerId == me.Id));
            if (c == null) return NotFound();

            body = body?.Trim();
            if (string.IsNullOrEmpty(body))
            {
                TempData["Error"] = "اكتب رسالة الأول.";
                return RedirectToAction(nameof(Thread), new { id });
            }
            if (body.Length > MaxBody) body = body[..MaxBody];

            // the other side must still be allowed to use the site
            var otherId = c.BuyerId == me.Id ? c.SellerId : c.BuyerId;
            var otherBlocked = await _users.Users.AnyAsync(u => u.Id == otherId && u.IsBlocked);
            if (otherBlocked)
            {
                TempData["Error"] = "مينفعش تبعت رسالة لحساب موقوف.";
                return RedirectToAction(nameof(Thread), new { id });
            }

            var since = DateTime.UtcNow.AddMinutes(-1);
            if (await _db.Messages.CountAsync(m => m.SenderId == me.Id && m.SentAt > since) >= MaxPerMinute)
            {
                TempData["Error"] = "بتبعت رسايل كتير بسرعة. استنى شوية.";
                return RedirectToAction(nameof(Thread), new { id });
            }

            var now = DateTime.UtcNow;
            _db.Messages.Add(new Message { ConversationId = id, SenderId = me.Id, Body = body, SentAt = now });
            c.LastMessageAt = now;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Thread), new { id });
        }

        // JS polls this every few seconds for messages newer than `after`
        [HttpGet]
        public async Task<IActionResult> Poll(int id, int after)
        {
            var uid = _users.GetUserId(User)!;
            var allowed = await _db.Conversations.AnyAsync(x => x.Id == id && (x.BuyerId == uid || x.SellerId == uid));
            if (!allowed) return NotFound();

            var fresh = await _db.Messages.AsNoTracking()
                .Where(m => m.ConversationId == id && m.Id > after)
                .OrderBy(m => m.Id).Take(100).ToListAsync();

            if (fresh.Any(m => m.SenderId != uid)) await MarkReadAsync(id, uid);

            return Json(fresh.Select(m => ToItem(m, uid)));
        }

        // ---------- helpers ----------
        private Task<int> MarkReadAsync(int conversationId, string uid) =>
            _db.Messages.Where(m => m.ConversationId == conversationId && m.SenderId != uid && m.ReadAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.ReadAt, DateTime.UtcNow));

        private static MessageItemVM ToItem(Message m, string uid) => new()
        {
            Id = m.Id,
            Mine = m.SenderId == uid,
            Body = m.Body,
            SentAtIso = DateTime.SpecifyKind(m.SentAt, DateTimeKind.Utc).ToString("o")
        };
    }
}
