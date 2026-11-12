using System.ComponentModel.DataAnnotations;

namespace LivestockMarket.ViewModels
{
    public class RegisterVM
    {
        [Required(ErrorMessage = "الاسم مطلوب"), StringLength(100)]
        [Display(Name = "الاسم بالكامل")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب"), EmailAddress(ErrorMessage = "بريد غير صحيح")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الموبايل مطلوب")]
        [RegularExpression(@"^01[0125][0-9]{8}$", ErrorMessage = "رقم موبايل مصري صحيح (11 رقم يبدأ بـ 01)")]
        [Display(Name = "رقم الموبايل")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "المحافظة")]
        public string? Governorate { get; set; }

        [Required(ErrorMessage = "كلمة المرور مطلوبة"), DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "كلمة المرور 8 أحرف على الأقل")]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "كلمتا المرور غير متطابقتين")]
        [Display(Name = "تأكيد كلمة المرور")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginVM
    {
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب"), EmailAddress(ErrorMessage = "بريد غير صحيح")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة"), DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "تذكرني")]
        public bool RememberMe { get; set; }
    }
}
