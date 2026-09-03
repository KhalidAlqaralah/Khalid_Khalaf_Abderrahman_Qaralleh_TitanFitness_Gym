using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TitanFitness.Domain.Branches;

namespace TitanFitness.Infrastructure.Persistence.Configurations;

public sealed class StudioConfiguration : IEntityTypeConfiguration<Studio>
{
    public void Configure(EntityTypeBuilder<Studio> builder)
    {
        builder.ToTable("Studios");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Capacity).IsRequired();

        builder.HasIndex(s => new { s.BranchId, s.Name }).IsUnique();
    }
}