using LivestockMarket.Data;
using LivestockMarket.Models;
using LivestockMarket.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LivestockMarket.Controllers
{
    // "Contact the administration": the page is public, sending a message needs a login (keeps spam out)
    public class ContactController : Controller
    {
        private const int MaxPerDay = 5;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public ContactController(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var vm = new ContactFormVM();
            if (User.Identity?.IsAuthenticated == true)
            {
                var me = await _users.GetUserAsync(User);
                vm.Phone = me?.PhoneNumber;
            }
            return View(vm);
        }

        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactFormVM vm)
        {
            var me = await _users.GetUserAsync(User);
            if (me == null) return Challenge();

            var since = DateTime.UtcNow.AddDays(-1);
            if (await _db.ContactMessages.CountAsync(m => m.UserId == me.Id && m.CreatedAt > since) >= MaxPerDay)
                ModelState.AddModelError("", "وصلت للحد الأقصى من الرسايل النهاردة. هنرد على رسايلك الأول.");

            if (!ModelState.IsValid) return View(vm);

            _db.ContactMessages.Add(new ContactMessage
            {
                UserId = me.Id,
                Subject = vm.Subject.Trim(),
                Message = vm.Message.Trim(),
                Phone = string.IsNullOrWhiteSpace(vm.Phone) ? null : vm.Phone.Trim()
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = "وصلت رسالتك للإدارة، هنرد عليك بأسرع وقت.";
            return RedirectToAction(nameof(Index));
        }
    }
}
