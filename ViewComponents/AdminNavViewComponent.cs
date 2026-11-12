using LivestockMarket.Data;
using LivestockMarket.Models;
using LivestockMarket.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LivestockMarket.ViewComponents
{
    // the bar with all admin sections + red counters; rendered by the layout on every admin page
    public class AdminNavViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;
        public AdminNavViewComponent(ApplicationDbContext db) => _db = db;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var vm = new AdminNavVM
            {
                Controller = ViewContext.RouteData.Values["controller"]?.ToString() ?? "",
                Action = ViewContext.RouteData.Values["action"]?.ToString() ?? "",
                PendingListings = await _db.Listings.CountAsync(l => l.Status == ListingStatus.PendingReview),
                OpenReports = await _db.Reports.CountAsync(r => r.Status == ReportStatus.Open),
                OpenChatReports = await _db.ConversationReports.CountAsync(r => r.Status == ReportStatus.Open),
                NewFeedback = await _db.AppFeedbacks.CountAsync(f => !f.IsRead),
                NewContacts = await _db.ContactMessages.CountAsync(m => !m.IsHandled)
            };
            return View(vm);
        }
    }
}
