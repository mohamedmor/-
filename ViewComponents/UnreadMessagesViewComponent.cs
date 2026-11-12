using System.Security.Claims;
using LivestockMarket.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LivestockMarket.ViewComponents
{
    // the red number next to "الرسائل" in the navbar
    public class UnreadMessagesViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;
        public UnreadMessagesViewComponent(ApplicationDbContext db) => _db = db;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var uid = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (uid == null) return View(0);

            var count = await _db.Messages.AsNoTracking().CountAsync(m =>
                m.SenderId != uid && m.ReadAt == null &&
                (m.Conversation!.BuyerId == uid || m.Conversation.SellerId == uid));
            return View(count);
        }
    }
}
