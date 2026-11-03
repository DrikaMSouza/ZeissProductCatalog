namespace ProductCatalog.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool ApplyMigrationsOnStartup { get; init; }

    public bool SeedSampleData { get; init; }
}
