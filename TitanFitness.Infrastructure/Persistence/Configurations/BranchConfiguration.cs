using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TitanFitness.Domain.Branches;

namespace TitanFitness.Infrastructure.Persistence.Configurations;

public sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Name).HasMaxLength(50).IsRequired();
        builder.Property(b => b.Address).HasMaxLength(200);

        builder.OwnsOne(b => b.Hours, hours =>
        {
            hours.Property(h => h.Opens).HasColumnName("OpensAt").IsRequired();
            hours.Property(h => h.Closes).HasColumnName("ClosesAt").IsRequired();
        });
        builder.Navigation(b => b.Hours).IsRequired();

        builder.HasMany(b => b.Studios)
               .WithOne()
               .HasForeignKey(s => s.BranchId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.Studios).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}