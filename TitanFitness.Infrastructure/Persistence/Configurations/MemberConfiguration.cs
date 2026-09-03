using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TitanFitness.Domain.Members;

namespace TitanFitness.Infrastructure.Persistence.Configurations;

public sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Number)
               .HasConversion(new MembershipNumberConverter())
               .HasMaxLength(10)
               .IsRequired();

        builder.HasIndex(m => m.Number).IsUnique();

        builder.Property(m => m.FullName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Email).HasMaxLength(100);
        builder.Property(m => m.Phone).HasMaxLength(20);
        builder.Property(m => m.Address).HasMaxLength(200);
        builder.Property(m => m.PhotoUrl).HasMaxLength(500);
        builder.Property(m => m.JoinedOn).IsRequired();

        builder.HasIndex(m => m.HomeBranchId);
    }
}