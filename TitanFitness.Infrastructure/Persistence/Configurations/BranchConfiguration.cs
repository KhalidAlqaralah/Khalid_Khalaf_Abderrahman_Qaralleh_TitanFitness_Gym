using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TitanFitness.Domain.Branches;

namespace TitanFitness.Infrastructure.Persistence.Configurations;

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Name).HasMaxLength(Branch.NameMaxLength).IsRequired();
        builder.HasIndex(b => b.Name).IsUnique();
        builder.Property(b => b.Address).HasMaxLength(Branch.AddressMaxLength);

        // Value objects without identity are complex types: plain columns on the owner's table.
        builder.ComplexProperty(b => b.Hours, hours =>
        {
            hours.Property(h => h.Opens).HasColumnName("OpensAt");
            hours.Property(h => h.Closes).HasColumnName("ClosesAt");
        });

        // Branch (1) -> Studios (many). Studios belong to the branch aggregate and go with it.
        builder.HasMany(b => b.Studios)
            .WithOne()
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.Studios)
            .HasField("_studios")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();

        builder.Property<byte[]>(ConfigurationConstants.RowVersion).IsRowVersion();
    }
}

internal sealed class StudioConfiguration : IEntityTypeConfiguration<Studio>
{
    public void Configure(EntityTypeBuilder<Studio> builder)
    {
        builder.ToTable("Studios");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name).HasMaxLength(Studio.NameMaxLength).IsRequired();
        builder.HasIndex(s => new { s.BranchId, s.Name }).IsUnique();
    }
}
