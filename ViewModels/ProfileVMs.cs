using System.ComponentModel.DataAnnotations;

namespace LivestockMarket.ViewModels
{
    public class ProfileVM
    {
        [Required(ErrorMessage = "الاسم مطلوب"), StringLength(100)]
        [Display(Name = "الاسم بالكامل")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الموبايل مطلوب")]
        [RegularExpression(@"^01[0125][0-9]{8}$", ErrorMessage = "رقم موبايل مصري صحيح (11 رقم يبدأ بـ 01)")]
        [Display(Name = "رقم الموبايل")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "المحافظة")]
        public string? Governorate { get; set; }

        [StringLength(300, ErrorMessage = "النبذة 300 حرف بحد أقصى")]
        [Display(Name = "نبذة عنك")]
        public string? Bio { get; set; }

        public IFormFile? Avatar { get; set; }
        public bool RemoveAvatar { get; set; }
    }

    public class ChangePasswordVM
    {
        [Required(ErrorMessage = "اكتب كلمة المرور الحالية"), DataType(DataType.Password)]
        [Display(Name = "كلمة المرور الحالية")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "اكتب كلمة المرور الجديدة"), DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "كلمة المرور 8 أحرف على الأقل")]
        [Display(Name = "كلمة المرور الجديدة")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "كلمتا المرور غير متطابقتين")]
        [Display(Name = "تأكيد كلمة المرور الجديدة")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class ProfilePageVM
    {
        public ProfileVM Profile { get; set; } = new();
        public ChangePasswordVM Password { get; set; } = new();

        // display only
        public string Email { get; set; } = string.Empty;
        public string? ProfileImagePath { get; set; }
        public DateTime MemberSince { get; set; }
        public int ActiveListings { get; set; }
        public int PendingListings { get; set; }
        public int SoldListings { get; set; }
    }
}
