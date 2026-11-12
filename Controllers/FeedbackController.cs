using LivestockMarket.Data;
using LivestockMarket.Models;
using LivestockMarket.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LivestockMarket.Controllers
{
    // What users think of the site itself
    [Authorize]
    public class FeedbackController : Controller
    {
        private const int MaxPerDay = 5;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public FeedbackController(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        [HttpGet]
        public IActionResult Create() => View(new FeedbackFormVM());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FeedbackFormVM vm)
        {
            var uid = _users.GetUserId(User)!;
            if (!Enum.IsDefined(typeof(FeedbackKind), vm.Kind)) ModelState.AddModelError(nameof(vm.Kind), "نوع غير صحيح");

            var since = DateTime.UtcNow.AddDays(-1);
            if (await _db.AppFeedbacks.CountAsync(f => f.UserId == uid && f.CreatedAt > since) >= MaxPerDay)
                ModelState.AddModelError("", "وصلت للحد الأقصى من الآراء النهاردة. شكراً لتفاعلك!");

            if (!ModelState.IsValid) return View(vm);

            _db.AppFeedbacks.Add(new AppFeedback { UserId = uid, Rating = vm.Rating, Kind = vm.Kind, Message = vm.Message.Trim() });
            await _db.SaveChangesAsync();

            TempData["Success"] = "شكراً على رأيك، بنقراه كله ونحسّن بيه الموقع.";
            return RedirectToAction("Index", "Home");
        }
    }
}
