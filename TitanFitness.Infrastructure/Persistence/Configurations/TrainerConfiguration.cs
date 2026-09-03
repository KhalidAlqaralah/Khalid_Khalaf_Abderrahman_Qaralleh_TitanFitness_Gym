using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TitanFitness.Domain.Trainers;

namespace TitanFitness.Infrastructure.Persistence.Configurations;

public sealed class TrainerConfiguration : IEntityTypeConfiguration<Trainer>
{
    public void Configure(EntityTypeBuilder<Trainer> builder)
    {
        builder.ToTable("Trainers");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Email).HasMaxLength(100);
        builder.Property(t => t.Phone).HasMaxLength(20);
        builder.Property(t => t.IsActive).IsRequired();
    }
}