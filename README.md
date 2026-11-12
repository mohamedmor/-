# سوق المواشي (LivestockMarket)

ASP.NET Core 8 MVC + SQL Server + Identity.

## أول تشغيل
1. عدّل `ConnectionStrings:DefaultConnection` في appsettings.json لو السيرفر عندك مش `Server=.`
2. حساب الـ Admin (من غير ما تكتب الباسورد في الكود):
   ```
   dotnet user-secrets set "Seed:AdminEmail" "you@example.com"
   dotnet user-secrets set "Seed:AdminPassword" "كلمة-مرور-قوية-8+"
   ```
3. اعمل الـ migration الأول مرة:
   ```
   dotnet ef migrations add InitialCreate
   ```
4. شغّل: `dotnet run` (الـ seeder بيعمل الداتابيز والأقسام والـ Admin تلقائياً)
