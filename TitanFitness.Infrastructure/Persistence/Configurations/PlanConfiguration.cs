using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TitanFitness.Domain.Plans;

namespace TitanFitness.Infrastructure.Persistence.Configurations;

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Plans");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(50).IsRequired();
        builder.Property(p => p.IsPublished).IsRequired();

        builder.OwnsOne(p => p.Terms, terms =>
        {
            terms.Property(t => t.Price)
                 .HasConversion(new MoneyConverter())
                 .HasColumnType("decimal(18,2)")
                 .HasColumnName("Price")
                 .IsRequired();

            terms.Property(t => t.DurationInMonths).HasColumnName("DurationInMonths").IsRequired();
            terms.Property(t => t.MaxFreezeDays).HasColumnName("MaxFreezeDays").IsRequired();
            terms.Property(t => t.MaxFreezes).HasColumnName("MaxFreezes").IsRequired();
            terms.Property(t => t.GuestPassQuota).HasColumnName("GuestPassQuota").IsRequired();
            terms.Property(t => t.AccessScope).HasColumnName("AccessScope").IsRequired();
        });
        builder.Navigation(p => p.Terms).IsRequired();
    }
}