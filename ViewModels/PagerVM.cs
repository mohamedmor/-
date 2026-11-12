namespace LivestockMarket.ViewModels
{
    // Everything the shared pager (Views/Shared/_Pager.cshtml) needs
    public class PagerVM
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = "Index";

        // every query-string value that must survive when the page number changes (filters, sort, status, id ...)
        public Dictionary<string, string?> Route { get; set; } = new();

        public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
        public int From => Total == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Total, Page * PageSize);

        public static int Clamp(int page, int total, int pageSize)
        {
            var pages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            return Math.Clamp(page, 1, pages);
        }

        // page numbers to show; 0 means "..."   e.g. 1 0 4 5 [6] 7 8 0 20
        public List<int> Window(int around = 2)
        {
            var list = new List<int>();
            var last = TotalPages;
            int prev = 0;
            for (var p = 1; p <= last; p++)
            {
                if (p == 1 || p == last || (p >= Page - around && p <= Page + around))
                {
                    if (prev != 0 && p - prev > 1) list.Add(0);
                    list.Add(p);
                    prev = p;
                }
            }
            return list;
        }
    }
}
