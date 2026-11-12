using LivestockMarket.Models;
using LivestockMarket.Utilities;
using LivestockMarket.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LivestockMarket.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;

        public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
        {
            _users = users;
            _signIn = signIn;
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterVM());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // phone numbers identify sellers to buyers, so keep them unique
            if (_users.Users.Any(u => u.PhoneNumber == vm.Phone))
            {
                ModelState.AddModelError(nameof(vm.Phone), "رقم الموبايل مسجل بالفعل");
                return View(vm);
            }

            var user = new ApplicationUser
            {
                UserName = vm.Email.Trim(),
                Email = vm.Email.Trim(),
                PhoneNumber = vm.Phone,
                FullName = vm.FullName.Trim(),
                Governorate = vm.Governorate
            };

            var result = await _users.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            // every self-registered account is a normal user: it can both buy and sell
            await _users.AddToRoleAsync(user, Roles.User);

            await _signIn.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginVM());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM vm, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(vm);

            var user = await _users.FindByEmailAsync(vm.Email.Trim());
            if (user == null || user.IsBlocked)
            {
                ModelState.AddModelError("", "البريد الإلكتروني أو كلمة المرور غير صحيحة");
                return View(vm);
            }

            var result = await _signIn.PasswordSignInAsync(user, vm.Password, vm.RememberMe, lockoutOnFailure: true);
            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "محاولات كثيرة، حاول مرة أخرى بعد قليل");
                return View(vm);
            }
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "البريد الإلكتروني أو كلمة المرور غير صحيحة");
                return View(vm);
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}
