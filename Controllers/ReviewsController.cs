using LivestockMarket.Data;
using LivestockMarket.Models;
using LivestockMarket.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LivestockMarket.Controllers
{
    // Rating + comment about a seller. Only someone who actually talked to the seller can review him.
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public ReviewsController(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        [HttpGet]
        public async Task<IActionResult> Seller(string id)
        {
            var me = _users.GetUserId(User)!;
            var (seller, error) = await CheckAsync(id, me);
            if (seller == null) return NotFound();
            if (error != null)
            {
                TempData["Error"] = error;
                return RedirectToAction("Profile", "Sellers", new { id });
            }

            var existing = await _db.SellerReviews.AsNoTracking().FirstOrDefaultAsync(r => r.SellerId == id && r.ReviewerId == me);
            return View(new ReviewFormVM
            {
                SellerId = id,
                SellerName = seller.FullName,
                IsEdit = existing != null,
                Rating = existing?.Rating ?? 0,
                Comment = existing?.Comment
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Seller(ReviewFormVM vm)
        {
            var me = _users.GetUserId(User)!;
            var (seller, error) = await CheckAsync(vm.SellerId, me);
            if (seller == null) return NotFound();
            if (error != null)
            {
                TempData["Error"] = error;
                return RedirectToAction("Profile", "Sellers", new { id = vm.SellerId });
            }

            vm.SellerName = seller.FullName;
            if (!ModelState.IsValid) return View(vm);

            var comment = string.IsNullOrWhiteSpace(vm.Comment) ? null : vm.Comment.Trim();
            var review = await _db.SellerReviews.FirstOrDefaultAsync(r => r.SellerId == vm.SellerId && r.ReviewerId == me);
            if (review == null)
            {
                _db.SellerReviews.Add(new SellerReview { SellerId = vm.SellerId, ReviewerId = me, Rating = vm.Rating, Comment = comment });
            }
            else
            {
                review.Rating = vm.Rating;
                review.Comment = comment;
                review.UpdatedAt = DateTime.UtcNow;
            }

            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException) { /* double submit: the review already exists */ }

            TempData["Success"] = "شكراً، تقييمك اتحفظ.";
            return RedirectToAction("Profile", "Sellers", new { id = vm.SellerId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string sellerId)
        {
            var me = _users.GetUserId(User)!;
            var review = await _db.SellerReviews.FirstOrDefaultAsync(r => r.SellerId == sellerId && r.ReviewerId == me);
            if (review != null)
            {
                _db.SellerReviews.Remove(review);
                await _db.SaveChangesAsync();
                TempData["Success"] = "تم حذف تقييمك.";
            }
            return RedirectToAction("Profile", "Sellers", new { id = sellerId });
        }

        // seller == null -> 404; error != null -> show it and go back
        private async Task<(ApplicationUser? seller, string? error)> CheckAsync(string sellerId, string me)
        {
            var seller = await _users.FindByIdAsync(sellerId);
            if (seller == null || seller.IsBlocked) return (null, null);
            if (seller.Id == me) return (seller, "مينفعش تقيّم نفسك.");

            var talked = await _db.Conversations.AnyAsync(c => c.BuyerId == me && c.SellerId == sellerId);
            if (!talked) return (seller, "تقدر تقيّم البائع بعد ما تتواصل معاه من خلال الرسائل.");
            return (seller, null);
        }
    }
}
