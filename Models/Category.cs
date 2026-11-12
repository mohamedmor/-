using System.ComponentModel.DataAnnotations;

namespace LivestockMarket.Models
{
    // Cows, buffalo, sheep, goats, dogs, cats ... the Admin can add more later
    public class Category
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(80)]
        public string Slug { get; set; } = string.Empty;

        public Section Section { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public List<Listing> Listings { get; set; } = new();
    }
}
