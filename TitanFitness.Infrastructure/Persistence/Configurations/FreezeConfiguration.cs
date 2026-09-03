using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Infrastructure.Persistence.Configurations;

public sealed class FreezeConfiguration : IEntityTypeConfiguration<Freeze>
{
    public void Configure(EntityTypeBuilder<Freeze> builder)
    {
        builder.ToTable("Freezes");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.OwnsOne(f => f.Period, period =>
        {
            period.Property(p => p.Start).HasColumnName("StartDate").IsRequired();
            period.Property(p => p.End).HasColumnName("EndDate").IsRequired();
        });
        builder.Navigation(f => f.Period).IsRequired();

        builder.Property(f => f.DurationInMonths).IsRequired();
        builder.Property(f => f.Reason).IsRequired();
        builder.Property(f => f.Notes).HasMaxLength(200);
        builder.Property(f => f.RequestedOn).IsRequired();

        builder.HasIndex(f => f.MembershipId);
    }
}