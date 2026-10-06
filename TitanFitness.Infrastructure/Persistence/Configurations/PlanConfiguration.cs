using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.Plans;

namespace TitanFitness.Infrastructure.Persistence.Configurations;

internal sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Plans");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(Plan.NameMaxLength).IsRequired();
        builder.HasIndex(p => p.Name).IsUnique();

        builder.ComplexProperty(p => p.Terms, terms => TermsMapping.Map(terms, prefix: ""));

        builder.Property<byte[]>(ConfigurationConstants.RowVersion).IsRowVersion();
    }
}

/// <summary>The same column layout for the terms a plan sells and the terms a membership was sold.</summary>
internal static class TermsMapping
{
    public static void Map(ComplexPropertyBuilder<MembershipTerms> terms, string prefix)
    {
        // Money is a nested complex type, so queries can filter and sort on Price.Amount in SQL.
        terms.ComplexProperty(t => t.Price, price =>
            price.Property(m => m.Amount).HasColumnName($"{prefix}Price").HasPrecision(10, 2));

        terms.Property(t => t.DurationInMonths).HasColumnName($"{prefix}DurationInMonths");
        terms.Property(t => t.MaxFreezeDays).HasColumnName($"{prefix}MaxFreezeDays");
        terms.Property(t => t.MaxFreezes).HasColumnName($"{prefix}MaxFreezes");
        terms.Property(t => t.GuestPassQuota).HasColumnName($"{prefix}GuestPassQuota");
        terms.Property(t => t.AccessScope).HasColumnName($"{prefix}AccessScope");
    }
}
