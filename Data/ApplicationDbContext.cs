using LivestockMarket.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LivestockMarket.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<AnimalCondition> AnimalConditions => Set<AnimalCondition>();
        public DbSet<Listing> Listings => Set<Listing>();
        public DbSet<ListingImage> ListingImages => Set<ListingImage>();
        public DbSet<ListingAttribute> ListingAttributes => Set<ListingAttribute>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<ConversationReport> ConversationReports => Set<ConversationReport>();
        public DbSet<SellerReview> SellerReviews => Set<SellerReview>();
        public DbSet<AppFeedback> AppFeedbacks => Set<AppFeedback>();
        public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
        public DbSet<Conversation> Conversations => Set<Conversation>();
        public DbSet<Message> Messages => Set<Message>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<Category>().HasIndex(c => c.Slug).IsUnique();

            b.Entity<Listing>(e =>
            {
                e.Property(x => x.Price).HasPrecision(18, 2);
                e.Property(x => x.WeightKg).HasPrecision(8, 2);
                e.Property(x => x.Quantity).HasDefaultValue(1);

                // a seller can't be deleted while he still has listings
                e.HasOne(x => x.Seller).WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Category).WithMany(c => c.Listings).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Condition).WithMany().HasForeignKey(x => x.ConditionId).OnDelete(DeleteBehavior.SetNull);

                // the public list always filters by status and sorts by date
                e.HasIndex(x => new { x.Status, x.CategoryId, x.PublishedAt });
            });

            b.Entity<Report>(e =>
            {
                e.HasOne(r => r.Listing).WithMany().HasForeignKey(r => r.ListingId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(r => r.Reporter).WithMany().HasForeignKey(r => r.ReporterId).OnDelete(DeleteBehavior.Restrict);
                // one report per user per listing
                e.HasIndex(r => new { r.ListingId, r.ReporterId }).IsUnique();
                e.HasIndex(r => r.Status);
            });

            b.Entity<ConversationReport>(e =>
            {
                e.HasOne(r => r.Reporter).WithMany().HasForeignKey(r => r.ReporterId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(r => r.ReportedUser).WithMany().HasForeignKey(r => r.ReportedUserId).OnDelete(DeleteBehavior.Restrict);
                e.Property(r => r.Snapshot).HasMaxLength(8000);
                e.HasIndex(r => new { r.ConversationId, r.ReporterId }).IsUnique();
                e.HasIndex(r => r.Status);
            });

            b.Entity<SellerReview>(e =>
            {
                e.HasOne(r => r.Seller).WithMany().HasForeignKey(r => r.SellerId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(r => r.Reviewer).WithMany().HasForeignKey(r => r.ReviewerId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(r => new { r.SellerId, r.ReviewerId }).IsUnique();
                e.ToTable(t => t.HasCheckConstraint("CK_SellerReview_Rating", "[Rating] BETWEEN 1 AND 5"));
            });

            b.Entity<AppFeedback>(e =>
            {
                e.HasOne(f => f.User).WithMany().HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Restrict);
                e.ToTable(t => t.HasCheckConstraint("CK_AppFeedback_Rating", "[Rating] BETWEEN 1 AND 5"));
            });

            b.Entity<ContactMessage>(e =>
            {
                e.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(m => m.IsHandled);
            });

            b.Entity<Conversation>(e =>
            {
                e.HasOne(c => c.Listing).WithMany().HasForeignKey(c => c.ListingId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(c => c.Buyer).WithMany().HasForeignKey(c => c.BuyerId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(c => c.Seller).WithMany().HasForeignKey(c => c.SellerId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(c => new { c.ListingId, c.BuyerId }).IsUnique();
                e.HasIndex(c => c.BuyerId);
                e.HasIndex(c => c.SellerId);
            });

            b.Entity<Message>(e =>
            {
                e.HasOne(m => m.Conversation).WithMany(c => c.Messages).HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(m => new { m.ConversationId, m.Id });
            });

            b.Entity<ListingImage>().HasOne(i => i.Listing).WithMany(l => l.Images).HasForeignKey(i => i.ListingId).OnDelete(DeleteBehavior.Cascade);
            b.Entity<ListingAttribute>().HasOne(a => a.Listing).WithMany(l => l.Attributes).HasForeignKey(a => a.ListingId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
