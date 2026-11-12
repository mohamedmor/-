namespace LivestockMarket.Services
{
    // Saves listing photos safely: checks size, extension AND the real file signature,
    // and always stores under a random name (never the name the user sent).
    public class ImageStorage
    {
        public const int MaxFileBytes = 5 * 1024 * 1024;   // 5 MB per photo
        public const int MaxImagesPerListing = 6;
        public const string ListingsFolder = "uploads/listings";
        public const string AvatarsFolder = "uploads/avatars";
        private static readonly string[] Folders = { ListingsFolder, AvatarsFolder };

        private static readonly string[] AllowedExt = { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly string _root;

        public ImageStorage(IWebHostEnvironment env)
        {
            var web = env.WebRootPath;
            if (string.IsNullOrEmpty(web))
                web = Path.Combine(env.ContentRootPath, "wwwroot");
            _root = web;
        }

        // returns null when the file is fine, otherwise an Arabic error message
        public async Task<string?> ValidateAsync(IFormFile file)
        {
            if (file.Length == 0) return $"الملف {file.FileName} فارغ";
            if (file.Length > MaxFileBytes) return $"الصورة {file.FileName} أكبر من 5 ميجا";

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExt.Contains(ext)) return $"نوع الملف {file.FileName} غير مسموح (jpg / png / webp فقط)";

            var head = new byte[12];
            await using var s = file.OpenReadStream();
            var read = await s.ReadAsync(head, 0, head.Length);
            if (read < 12 || !LooksLikeImage(head)) return $"الملف {file.FileName} ليس صورة صحيحة";
            return null;
        }

        private static bool LooksLikeImage(byte[] h)
        {
            bool jpg = h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF;
            bool png = h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47;
            bool webp = h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F'
                     && h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P';
            return jpg || png || webp;
        }

        // call only after ValidateAsync returned null; returns the web path like /uploads/listings/abc.jpg
        public async Task<string> SaveAsync(IFormFile file, string folder = ListingsFolder)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var name = Guid.NewGuid().ToString("N") + ext;
            var dir = Path.Combine(_root, folder);
            Directory.CreateDirectory(dir);
            await using var fs = new FileStream(Path.Combine(dir, name), FileMode.Create);
            await file.CopyToAsync(fs);
            return "/" + folder + "/" + name;
        }

        public void Delete(string? webPath)
        {
            if (string.IsNullOrEmpty(webPath)) return;
            // only ever delete inside our own uploads folders
            var folder = Folders.FirstOrDefault(f => webPath.StartsWith("/" + f + "/", StringComparison.Ordinal));
            if (folder == null) return;
            var name = Path.GetFileName(webPath);
            var full = Path.Combine(_root, folder, name);
            try { if (File.Exists(full)) File.Delete(full); } catch { /* best effort */ }
        }
    }
}
