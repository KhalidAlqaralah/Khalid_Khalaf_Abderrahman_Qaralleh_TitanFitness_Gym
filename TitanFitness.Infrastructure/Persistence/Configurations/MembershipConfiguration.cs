using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Infrastructure.Persistence.Configurations;

public sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("Memberships");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.PurchasedOn).IsRequired();
        builder.Property(m => m.Status).IsRequired();

        builder.OwnsOne(m => m.Period, period =>
        {
            period.Property(p => p.Start).HasColumnName("StartDate").IsRequired();
            period.Property(p => p.End).HasColumnName("EndDate").IsRequired();
        });
        builder.Navigation(m => m.Period).IsRequired();

        builder.OwnsOne(m => m.Terms, terms =>
        {
            terms.Property(t => t.Price)
                 .HasConversion(new MoneyConverter())
                 .HasColumnType("decimal(18,2)")
                 .HasColumnName("AgreedPrice")
                 .IsRequired();

            terms.Property(t => t.DurationInMonths).HasColumnName("AgreedDurationInMonths").IsRequired();
            terms.Property(t => t.MaxFreezeDays).HasColumnName("AgreedMaxFreezeDays").IsRequired();
            terms.Property(t => t.MaxFreezes).HasColumnName("AgreedMaxFreezes").IsRequired();
            terms.Property(t => t.GuestPassQuota).HasColumnName("AgreedGuestPassQuota").IsRequired();
            terms.Property(t => t.AccessScope).HasColumnName("AgreedAccessScope").IsRequired();
        });
        builder.Navigation(m => m.Terms).IsRequired();

        builder.HasMany(m => m.Freezes)
               .WithOne()
               .HasForeignKey(f => f.MembershipId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.GuestPasses)
               .WithOne()
               .HasForeignKey(g => g.MembershipId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(m => m.Freezes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(m => m.GuestPasses).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(m => m.MemberId);
        builder.HasIndex(m => m.PlanId);
    }
}