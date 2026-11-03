using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductCatalog.Domain.Categories;
using ProductCatalog.Infrastructure.Persistence.Seeding;

namespace ProductCatalog.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(Category.NameMaxLength)
            .IsRequired();

        builder.HasIndex(c => c.Name).IsUnique();

        builder.Navigation(c => c.Products).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Reference data is part of the schema, so it is seeded through the migration itself.
        builder.HasData(CatalogSeedData.Categories);
    }
}
