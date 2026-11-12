using System.ComponentModel.DataAnnotations;

namespace LivestockMarket.Models
{
    // The kind/state of the animal: بكر, عشر, والدة, دكر, نتاية ...
    // Kept in a table (not in code) so new kinds can be added without a release.
    public class AnimalCondition
    {
        public int Id { get; set; }

        [Required, StringLength(60)]
        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
