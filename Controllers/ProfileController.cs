using LivestockMarket.Data;
using LivestockMarket.Models;
using LivestockMarket.Services;
using LivestockMarket.Utilities;
using LivestockMarket.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LivestockMarket.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly ImageStorage _images;

        public ProfileController(ApplicationDbContext db, UserManager<ApplicationUser> users,
                                 SignInManager<ApplicationUser> signIn, ImageStorage images)
        {
            _db = db;
            _users = users;
            _signIn = signIn;
            _images = images;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var me = await _users.GetUserAsync(User);
            if (me == null) return Challenge();
            return View(await BuildAsync(me, null, null));
        }

        [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Edit([Bind(Prefix = "Profile")] ProfileVM vm)
        {
            var me = await _users.GetUserAsync(User);
            if (me == null) return Challenge();

            if (!string.IsNullOrEmpty(vm.Governorate) && !Egypt.Governorates.Contains(vm.Governorate))
                ModelState.AddModelError("Profile.Governorate", "اختر المحافظة من القائمة");

            var phone = vm.Phone?.Trim();
            if (!string.IsNullOrEmpty(phone) &&
                await _users.Users.AnyAsync(u => u.PhoneNumber == phone && u.Id != me.Id))
                ModelState.AddModelError("Profile.Phone", "رقم الموبايل مستخدم في حساب تاني");

            var avatar = vm.Avatar != null && vm.Avatar.Length > 0 ? vm.Avatar : null;
            if (avatar != null)
            {
                if (avatar.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError("Profile.Avatar", "الصورة أكبر من 2 ميجا");
                else
                {
                    var err = await _images.ValidateAsync(avatar);
                    if (err != null) ModelState.AddModelError("Profile.Avatar", err);
                }
            }

            if (!ModelState.IsValid) return View("Index", await BuildAsync(me, vm, null));

            me.FullName = vm.FullName.Trim();
            me.PhoneNumber = phone;
            me.Governorate = string.IsNullOrEmpty(vm.Governorate) ? null : vm.Governorate;
            me.Bio = string.IsNullOrWhiteSpace(vm.Bio) ? null : vm.Bio.Trim();

            string? oldImage = null;
            if (avatar != null)
            {
                oldImage = me.ProfileImagePath;
                me.ProfileImagePath = await _images.SaveAsync(avatar, ImageStorage.AvatarsFolder);
            }
            else if (vm.RemoveAvatar)
            {
                oldImage = me.ProfileImagePath;
                me.ProfileImagePath = null;
            }

            var result = await _users.UpdateAsync(me);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View("Index", await BuildAsync(me, vm, null));
            }

            _images.Delete(oldImage);
            TempData["Success"] = "تم حفظ بياناتك.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] ChangePasswordVM vm)
        {
            var me = await _users.GetUserAsync(User);
            if (me == null) return Challenge();

            if (!ModelState.IsValid) return View("Index", await BuildAsync(me, null, vm));

            var result = await _users.ChangePasswordAsync(me, vm.CurrentPassword, vm.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("Password.CurrentPassword",
                        e.Code == "PasswordMismatch" ? "كلمة المرور الحالية غير صحيحة" : e.Description);
                return View("Index", await BuildAsync(me, null, vm));
            }

            // the security stamp changed: keep THIS browser logged in, other sessions are dropped
            await _signIn.RefreshSignInAsync(me);
            TempData["Success"] = "تم تغيير كلمة المرور.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<ProfilePageVM> BuildAsync(ApplicationUser me, ProfileVM? profile, ChangePasswordVM? pwd)
        {
            var mine = _db.Listings.AsNoTracking().Where(l => l.SellerId == me.Id);
            return new ProfilePageVM
            {
                Profile = profile ?? new ProfileVM
                {
                    FullName = me.FullName,
                    Phone = me.PhoneNumber ?? "",
                    Governorate = me.Governorate,
                    Bio = me.Bio
                },
                // never echo passwords back into the form
                Password = new ChangePasswordVM(),
                Email = me.Email ?? "",
                ProfileImagePath = me.ProfileImagePath,
                MemberSince = me.CreatedAt,
                ActiveListings = await mine.CountAsync(l => l.Status == ListingStatus.Published),
                PendingListings = await mine.CountAsync(l => l.Status == ListingStatus.PendingReview),
                SoldListings = await mine.CountAsync(l => l.Status == ListingStatus.Sold)
            };
        }
    }
}
