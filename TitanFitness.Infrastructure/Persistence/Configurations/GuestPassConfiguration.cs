using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Infrastructure.Persistence.Configurations;

public sealed class GuestPassConfiguration : IEntityTypeConfiguration<GuestPass>
{
    public void Configure(EntityTypeBuilder<GuestPass> builder)
    {
        builder.ToTable("GuestPasses");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();

        builder.Property(g => g.IssuedOn).IsRequired();
        builder.Property(g => g.GuestName).HasMaxLength(100);

        builder.HasIndex(g => g.MembershipId);
    }
}