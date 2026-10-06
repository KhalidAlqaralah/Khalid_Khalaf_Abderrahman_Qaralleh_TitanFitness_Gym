namespace TitanFitness.Infrastructure.Persistence;

internal static class ConfigurationConstants
{
    /// <summary>Optimistic concurrency token added to every aggregate root as a shadow property.</summary>
    public const string RowVersion = "RowVersion";
}
