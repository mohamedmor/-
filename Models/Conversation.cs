using System.ComponentModel.DataAnnotations;

namespace LivestockMarket.Models
{
    // One chat between a buyer and the seller of a listing (one per buyer per listing)
    public class Conversation
    {
        public int Id { get; set; }

        public int ListingId { get; set; }
        public Listing? Listing { get; set; }

        [Required] public string BuyerId { get; set; } = string.Empty;
        public ApplicationUser? Buyer { get; set; }

        [Required] public string SellerId { get; set; } = string.Empty;
        public ApplicationUser? Seller { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        public List<Message> Messages { get; set; } = new();
    }

    public class Message
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }
        public Conversation? Conversation { get; set; }

        [Required] public string SenderId { get; set; } = string.Empty;

        [Required, StringLength(1000)]
        public string Body { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // null = the other side hasn't opened it yet
        public DateTime? ReadAt { get; set; }
    }
}
