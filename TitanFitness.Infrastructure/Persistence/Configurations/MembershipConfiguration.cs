using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TitanFitness.Domain.Members;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.Plans;

namespace TitanFitness.Infrastructure.Persistence.Configurations;

internal sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("Memberships");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.ComplexProperty(m => m.Period, period =>
        {
            period.Property(p => p.Start).HasColumnName("StartDate");
            period.Property(p => p.End).HasColumnName("EndDate");
        });

        // The terms copied at purchase, in their own columns.
        builder.ComplexProperty(m => m.Terms, terms => TermsMapping.Map(terms, prefix: "Terms"));

        // Membership (many) -> Member (1).
        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(m => m.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // Membership (many) -> Plan (1): the plan it was sold from.
        builder.HasOne<Plan>()
            .WithMany()
            .HasForeignKey(m => m.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Membership (1) -> Freezes / GuestPasses (many): children of the aggregate.
        builder.HasMany(m => m.Freezes)
            .WithOne()
            .HasForeignKey(f => f.MembershipId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.GuestPasses)
            .WithOne()
            .HasForeignKey(g => g.MembershipId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(m => m.Freezes).HasField("_freezes").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
        builder.Navigation(m => m.GuestPasses).HasField("_guestPasses").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();

        builder.HasIndex(m => m.MemberId);

        builder.Property<byte[]>(ConfigurationConstants.RowVersion).IsRowVersion();
    }
}

internal sealed class FreezeConfiguration : IEntityTypeConfiguration<Freeze>
{
    public void Configure(EntityTypeBuilder<Freeze> builder)
    {
        builder.ToTable("Freezes");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.ComplexProperty(f => f.Period, period =>
        {
            period.Property(p => p.Start).HasColumnName("StartDate");
            period.Property(p => p.End).HasColumnName("EndDate");
        });

        builder.Property(f => f.Notes).HasMaxLength(Freeze.NotesMaxLength);

        builder.Ignore(f => f.EffectivePeriod);
    }
}

internal sealed class GuestPassConfiguration : IEntityTypeConfiguration<GuestPass>
{
    public void Configure(EntityTypeBuilder<GuestPass> builder)
    {
        builder.ToTable("GuestPasses");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();
        builder.Property(g => g.GuestName).HasMaxLength(GuestPass.GuestNameMaxLength);
    }
}
