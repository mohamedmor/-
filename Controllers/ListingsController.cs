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
    public class ListingsController : Controller
    {
        private const int MaxPendingPerUser = 10;

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly ImageStorage _images;

        public ListingsController(ApplicationDbContext db, UserManager<ApplicationUser> users, ImageStorage images)
        {
            _db = db;
            _users = users;
            _images = images;
        }

        // ---------- My listings ----------
        public async Task<IActionResult> My(int page = 1)
        {
            var uid = _users.GetUserId(User)!;
            const int size = 10;
            var mine = _db.Listings.AsNoTracking().Where(l => l.SellerId == uid);
            var total = await mine.CountAsync();
            page = PagerVM.Clamp(page, total, size);

            var rows = await mine
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * size).Take(size)
                .Select(l => new MyListingVM
                {
                    Id = l.Id,
                    Title = l.Title,
                    Price = l.Price,
                    PricingType = l.PricingType,
                    Quantity = l.Quantity,
                    Status = l.Status,
                    RejectionReason = l.RejectionReason,
                    ViewsCount = l.ViewsCount,
                    CreatedAt = l.CreatedAt,
                    MainImage = l.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.SortOrder)
                                        .Select(i => i.FilePath).FirstOrDefault()
                })
                .ToListAsync();
            return View(new MyListingsVM
            {
                Items = rows,
                Pager = new PagerVM { Page = page, PageSize = size, Total = total, Controller = "Listings", Action = "My" }
            });
        }

        // ---------- Create ----------
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (await IsBlockedAsync()) return Forbid();
            var vm = new ListingFormVM();
            await FillListsAsync(vm);
            var me = await _users.GetUserAsync(User);
            if (me?.Governorate != null) vm.Governorate = me.Governorate;
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(40_000_000)]
        public async Task<IActionResult> Create(ListingFormVM vm)
        {
            if (await IsBlockedAsync()) return Forbid();
            var uid = _users.GetUserId(User)!;

            var pending = await _db.Listings.CountAsync(l => l.SellerId == uid && l.Status == ListingStatus.PendingReview);
            if (pending >= MaxPendingPerUser)
                ModelState.AddModelError("", $"عندك {MaxPendingPerUser} إعلانات قيد المراجعة. استنى لحد ما تتراجع قبل ما تضيف غيرها.");

            await ValidateCommonAsync(vm);

            var files = vm.NewImages?.Where(f => f.Length > 0).ToList() ?? new List<IFormFile>();
            if (files.Count == 0) ModelState.AddModelError("NewImages", "ضيف صورة واحدة على الأقل");
            if (files.Count > ImageStorage.MaxImagesPerListing)
                ModelState.AddModelError("NewImages", $"الحد الأقصى {ImageStorage.MaxImagesPerListing} صور");
            await ValidateFilesAsync(files);

            if (!ModelState.IsValid)
            {
                await FillListsAsync(vm);
                return View(vm);
            }

            var listing = new Listing
            {
                SellerId = uid,
                Status = ListingStatus.PendingReview,
                CreatedAt = DateTime.UtcNow
            };
            Apply(vm, listing);

            var order = 0;
            foreach (var f in files)
            {
                listing.Images.Add(new ListingImage
                {
                    FilePath = await _images.SaveAsync(f),
                    IsMain = order == 0,
                    SortOrder = order++
                });
            }

            _db.Listings.Add(listing);
            await _db.SaveChangesAsync();

            TempData["Success"] = "تم إرسال إعلانك للمراجعة. هيظهر للناس بعد موافقة الإدارة.";
            return RedirectToAction(nameof(My));
        }

        // ---------- Edit ----------
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var listing = await LoadOwnedAsync(id);
            if (listing == null) return NotFound();
            if (listing.Status == ListingStatus.Sold || listing.Status == ListingStatus.Closed)
            {
                TempData["Error"] = "مينفعش تعدّل إعلان اتباع أو اتقفل.";
                return RedirectToAction(nameof(My));
            }

            var vm = new ListingFormVM
            {
                Id = listing.Id,
                Title = listing.Title,
                CategoryId = listing.CategoryId,
                ConditionId = listing.ConditionId,
                PricingType = listing.PricingType,
                Price = listing.Price,
                WeightKg = listing.WeightKg,
                Quantity = listing.Quantity,
                AgeMonths = listing.AgeMonths,
                Gender = listing.Gender,
                Governorate = listing.Governorate,
                City = listing.City,
                Description = listing.Description,
                Attributes = listing.Attributes.Select(a => new AttributeVM { Name = a.Name, Value = a.Value }).ToList(),
                ExistingImages = listing.Images.OrderBy(i => i.SortOrder).ToList()
            };
            await FillListsAsync(vm);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(40_000_000)]
        public async Task<IActionResult> Edit(int id, ListingFormVM vm)
        {
            if (await IsBlockedAsync()) return Forbid();
            var listing = await LoadOwnedAsync(id);
            if (listing == null) return NotFound();
            if (listing.Status == ListingStatus.Sold || listing.Status == ListingStatus.Closed)
                return RedirectToAction(nameof(My));

            await ValidateCommonAsync(vm);

            var removeIds = (vm.RemoveImageIds ?? new List<int>()).ToHashSet();
            var toRemove = listing.Images.Where(i => removeIds.Contains(i.Id)).ToList();
            var files = vm.NewImages?.Where(f => f.Length > 0).ToList() ?? new List<IFormFile>();
            var total = listing.Images.Count - toRemove.Count + files.Count;
            if (total < 1) ModelState.AddModelError("NewImages", "لازم تسيب صورة واحدة على الأقل");
            if (total > ImageStorage.MaxImagesPerListing)
                ModelState.AddModelError("NewImages", $"الحد الأقصى {ImageStorage.MaxImagesPerListing} صور");
            await ValidateFilesAsync(files);

            if (!ModelState.IsValid)
            {
                vm.Id = id;
                vm.ExistingImages = listing.Images.OrderBy(i => i.SortOrder).ToList();
                await FillListsAsync(vm);
                return View(vm);
            }

            Apply(vm, listing);

            // attributes: replace the whole set
            _db.ListingAttributes.RemoveRange(listing.Attributes);
            listing.Attributes = new List<ListingAttribute>();
            AddAttributes(vm, listing);

            // images
            var removedPaths = toRemove.Select(i => i.FilePath).ToList();
            _db.ListingImages.RemoveRange(toRemove);
            foreach (var r in toRemove) listing.Images.Remove(r);

            var next = listing.Images.Count == 0 ? 0 : listing.Images.Max(i => i.SortOrder) + 1;
            foreach (var f in files)
            {
                listing.Images.Add(new ListingImage { FilePath = await _images.SaveAsync(f), SortOrder = next++ });
            }
            if (!listing.Images.Any(i => i.IsMain))
            {
                var first = listing.Images.OrderBy(i => i.SortOrder).First();
                first.IsMain = true;
            }

            // any edit sends the listing back to review so a published ad can't be swapped for something else
            if (listing.Status == ListingStatus.Published || listing.Status == ListingStatus.Rejected
                || listing.Status == ListingStatus.Reserved)
            {
                listing.Status = ListingStatus.PendingReview;
                listing.RejectionReason = null;
                listing.PublishedAt = null;
            }

            await _db.SaveChangesAsync();
            foreach (var p in removedPaths) _images.Delete(p);

            TempData["Success"] = "تم حفظ التعديل. الإعلان رجع للمراجعة.";
            return RedirectToAction(nameof(My));
        }

        // ---------- Mark sold / close / delete ----------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkSold(int id)
        {
            var l = await _db.Listings.FirstOrDefaultAsync(x => x.Id == id && x.SellerId == _users.GetUserId(User));
            if (l == null) return NotFound();
            l.Status = ListingStatus.Sold;
            await _db.SaveChangesAsync();
            TempData["Success"] = "تم تحديد الإعلان كـ \"تم البيع\".";
            return RedirectToAction(nameof(My));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id)
        {
            var l = await _db.Listings.FirstOrDefaultAsync(x => x.Id == id && x.SellerId == _users.GetUserId(User));
            if (l == null) return NotFound();
            l.Status = ListingStatus.Closed;
            await _db.SaveChangesAsync();
            TempData["Success"] = "تم إيقاف الإعلان.";
            return RedirectToAction(nameof(My));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var l = await _db.Listings.Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id && x.SellerId == _users.GetUserId(User));
            if (l == null) return NotFound();
            var paths = l.Images.Select(i => i.FilePath).ToList();
            _db.Listings.Remove(l);
            await _db.SaveChangesAsync();
            foreach (var p in paths) _images.Delete(p);
            TempData["Success"] = "تم حذف الإعلان.";
            return RedirectToAction(nameof(My));
        }

        // the trader sold some of the animals: set how many are still available (0 = all sold)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            var l = await _db.Listings.FirstOrDefaultAsync(x => x.Id == id && x.SellerId == _users.GetUserId(User));
            if (l == null) return NotFound();

            if (l.Status == ListingStatus.Sold || l.Status == ListingStatus.Closed)
            {
                TempData["Error"] = "الإعلان ده اتباع أو اتقفل.";
                return RedirectToAction(nameof(My));
            }
            if (quantity < 0 || quantity > 1000)
            {
                TempData["Error"] = "العدد لازم يكون بين 0 و 1000.";
                return RedirectToAction(nameof(My));
            }

            l.Quantity = quantity;
            if (quantity == 0)
            {
                l.Status = ListingStatus.Sold;
                TempData["Success"] = "العدد وصل صفر، فالإعلان اتحدد \"تم البيع\".";
            }
            else
            {
                TempData["Success"] = "تم تحديث العدد المتاح.";
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(My));
        }

        // ---------- Public details ----------
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var l = await _db.Listings
                .Include(x => x.Category).Include(x => x.Condition)
                .Include(x => x.Images).Include(x => x.Attributes)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (l == null) return NotFound();

            var uid = _users.GetUserId(User);
            var isOwner = uid != null && uid == l.SellerId;
            var isAdmin = User.IsInRole(Roles.Admin);

            // only published ads are public; the owner and Admin can preview the rest
            var visible = l.Status == ListingStatus.Published || l.Status == ListingStatus.Reserved || l.Status == ListingStatus.Sold;
            if (!visible && !isOwner && !isAdmin) return NotFound();

            if (l.Status == ListingStatus.Published && !isOwner)
            {
                await _db.Listings.Where(x => x.Id == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.ViewsCount, x => x.ViewsCount + 1));
            }

            var seller = await _users.FindByIdAsync(l.SellerId);
            var ratings = _db.SellerReviews.AsNoTracking().Where(r => r.SellerId == l.SellerId && !r.IsHidden);
            var ratingCount = await ratings.CountAsync();
            var ratingAvg = ratingCount == 0 ? 0 : await ratings.AverageAsync(r => (double)r.Rating);
            return View(new ListingDetailsVM
            {
                Listing = l,
                SellerName = seller?.FullName ?? "بائع",
                // the phone is only shown to logged-in visitors (reduces scraping)
                SellerPhone = User.Identity?.IsAuthenticated == true ? seller?.PhoneNumber : null,
                IsOwner = isOwner,
                SellerRating = ratingAvg,
                SellerReviewCount = ratingCount
            });
        }

        // ---------- helpers ----------
        private async Task<bool> IsBlockedAsync()
        {
            var me = await _users.GetUserAsync(User);
            return me == null || me.IsBlocked;
        }

        private Task<Listing?> LoadOwnedAsync(int id)
        {
            var uid = _users.GetUserId(User);
            return _db.Listings.Include(l => l.Images).Include(l => l.Attributes)
                .FirstOrDefaultAsync(l => l.Id == id && l.SellerId == uid);
        }

        private async Task FillListsAsync(ListingFormVM vm)
        {
            vm.Categories = await _db.Categories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
            vm.Conditions = await _db.AnimalConditions.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
            if (vm.Attributes.Count == 0) vm.Attributes.Add(new AttributeVM());
        }

        private async Task ValidateCommonAsync(ListingFormVM vm)
        {
            if (!await _db.Categories.AnyAsync(c => c.Id == vm.CategoryId && c.IsActive))
                ModelState.AddModelError(nameof(vm.CategoryId), "القسم غير صحيح");

            if (vm.ConditionId != null && !await _db.AnimalConditions.AnyAsync(c => c.Id == vm.ConditionId && c.IsActive))
                ModelState.AddModelError(nameof(vm.ConditionId), "الحالة غير صحيحة");

            if (!Egypt.Governorates.Contains(vm.Governorate))
                ModelState.AddModelError(nameof(vm.Governorate), "اختر المحافظة من القائمة");

            if (!Enum.IsDefined(typeof(PricingType), vm.PricingType))
                ModelState.AddModelError(nameof(vm.PricingType), "طريقة التسعير غير صحيحة");

            if (vm.Attributes.Count(a => !string.IsNullOrWhiteSpace(a.Name)) > 15)
                ModelState.AddModelError("", "الحد الأقصى 15 مواصفة");
        }

        private async Task ValidateFilesAsync(List<IFormFile> files)
        {
            foreach (var f in files)
            {
                var err = await _images.ValidateAsync(f);
                if (err != null) ModelState.AddModelError("NewImages", err);
            }
        }

        // copy the form fields we allow onto the entity (never bind the entity directly: over-posting)
        private static void Apply(ListingFormVM vm, Listing l)
        {
            l.Title = vm.Title.Trim();
            l.CategoryId = vm.CategoryId;
            l.ConditionId = vm.ConditionId;
            l.PricingType = vm.PricingType;
            l.Price = vm.Price;
            l.WeightKg = vm.WeightKg;
            l.Quantity = vm.Quantity;
            l.AgeMonths = vm.AgeMonths;
            l.Gender = vm.Gender;
            l.Governorate = vm.Governorate;
            l.City = string.IsNullOrWhiteSpace(vm.City) ? null : vm.City.Trim();
            l.Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim();
            if (l.Id == 0) AddAttributes(vm, l);
        }

        private static void AddAttributes(ListingFormVM vm, Listing l)
        {
            foreach (var a in vm.Attributes)
            {
                if (string.IsNullOrWhiteSpace(a.Name) || string.IsNullOrWhiteSpace(a.Value)) continue;
                l.Attributes.Add(new ListingAttribute { Name = a.Name.Trim(), Value = a.Value.Trim() });
            }
        }
    }
}
