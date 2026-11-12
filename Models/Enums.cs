namespace LivestockMarket.Models
{
    public enum Section { Livestock = 1, Pets = 2, Other = 3 }

    // How the seller prices the animal
    public enum PricingType
    {
        Fixed = 1,   // one price for the animal (dairy, breeding, pets)
        PerKg = 2    // price per kilo (animals sold for meat)
    }

    public enum Gender { Male = 1, Female = 2 }

    public enum ListingStatus
    {
        PendingReview = 1,
        Published = 2,
        Rejected = 3,
        Reserved = 4,
        Sold = 5,
        Closed = 6
    }
}
