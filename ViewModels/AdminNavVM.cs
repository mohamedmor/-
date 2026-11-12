namespace LivestockMarket.ViewModels
{
    public class AdminNavVM
    {
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int PendingListings { get; set; }
        public int OpenReports { get; set; }
        public int OpenChatReports { get; set; }
        public int NewFeedback { get; set; }
        public int NewContacts { get; set; }
    }
}
