using LivestockMarket.Models;

namespace LivestockMarket.ViewModels
{
    public class ConversationRowVM
    {
        public int Id { get; set; }
        public string ListingTitle { get; set; } = string.Empty;
        public string OtherName { get; set; } = string.Empty;
        public string? OtherAvatar { get; set; }
        public string? LastText { get; set; }
        public DateTime LastAt { get; set; }
        public int Unread { get; set; }
    }

    public class MessageItemVM
    {
        public int Id { get; set; }
        public bool Mine { get; set; }
        public string Body { get; set; } = string.Empty;
        public string SentAtIso { get; set; } = string.Empty;   // always UTC with a Z, formatted in the browser
    }

    public class ThreadVM
    {
        public int ConversationId { get; set; }
        public int ListingId { get; set; }
        public string ListingTitle { get; set; } = string.Empty;
        public ListingStatus ListingStatus { get; set; }
        public string OtherId { get; set; } = string.Empty;
        public string OtherName { get; set; } = string.Empty;
        public string? OtherAvatar { get; set; }
        public bool IsBuyer { get; set; }
        public List<MessageItemVM> Messages { get; set; } = new();
        public int LastId => Messages.Count == 0 ? 0 : Messages.Max(m => m.Id);
    }
}
