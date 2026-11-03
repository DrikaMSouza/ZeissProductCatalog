using ProductCatalog.Domain.Categories;

namespace ProductCatalog.Infrastructure.Persistence.Seeding;

internal static class CatalogSeedData
{
    public const int SpectacleLenses = 1;
    public const int Frames = 2;
    public const int Sunglasses = 3;
    public const int ContactLenses = 4;
    public const int Accessories = 5;

    public static readonly Category[] Categories =
    [
        new(SpectacleLenses, "Spectacle Lenses"),
        new(Frames, "Frames"),
        new(Sunglasses, "Sunglasses"),
        new(ContactLenses, "Contact Lenses"),
        new(Accessories, "Accessories"),
    ];

    public static readonly SampleProduct[] Products =
    [
        new("LNS-SV-150", "Single Vision Lens 1.50", "Standard index single vision lens with anti-reflective coating.", 89.90m, 120, SpectacleLenses),
        new("LNS-SV-167", "Single Vision Lens 1.67", "High index thin single vision lens.", 159.00m, 75, SpectacleLenses),
        new("LNS-PRG-160", "Progressive Lens 1.60", "Progressive lens for clear vision at every distance.", 349.00m, 40, SpectacleLenses),
        new("LNS-BLU-150", "Blue Light Filter Lens", "Lens with a blue light filter for intensive screen use.", 119.00m, 8, SpectacleLenses),
        new("FRM-TIT-01", "Titanium Frame Classic", "Lightweight titanium full-rim frame.", 229.00m, 25, Frames),
        new("FRM-ACE-02", "Acetate Frame Round", "Round acetate frame in tortoise shell.", 149.00m, 0, Frames),
        new("FRM-KID-03", "Kids Flex Frame", "Flexible, durable frame for children.", 79.00m, 60, Frames),
        new("SUN-POL-01", "Polarized Sunglasses", "Polarized lenses with full UV400 protection.", 189.00m, 30, Sunglasses),
        new("SUN-SPT-02", "Sport Sunglasses", "Wrap-around sport sunglasses with grip temples.", 139.00m, 3, Sunglasses),
        new("CL-DAY-30", "Daily Contact Lenses (30 pack)", "Silicone hydrogel daily disposable lenses.", 34.90m, 300, ContactLenses),
        new("ACC-CLN-01", "Lens Cleaning Spray", "Alcohol-free cleaning spray, 30 ml.", 9.90m, 500, Accessories),
        new("ACC-CSE-02", "Hard Shell Case", "Protective hard shell glasses case.", 19.90m, 0, Accessories),
    ];

    internal sealed record SampleProduct(string Sku, string Name, string Description, decimal Price, int StockQuantity, int CategoryId);
}
