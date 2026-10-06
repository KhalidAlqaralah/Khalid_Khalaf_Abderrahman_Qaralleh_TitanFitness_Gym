using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.Trainers;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Infrastructure.Persistence.Configurations;

internal sealed class TrainerConfiguration : IEntityTypeConfiguration<Trainer>
{
    public void Configure(EntityTypeBuilder<Trainer> builder)
    {
        builder.ToTable("Trainers");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Code).HasMaxLength(Trainer.CodeMaxLength).IsRequired();
        builder.HasIndex(t => t.Code).IsUnique();

        builder.Property(t => t.Name).HasMaxLength(Trainer.NameMaxLength).IsRequired();
        builder.Property(t => t.Specialty).HasMaxLength(Trainer.SpecialtyMaxLength);

        builder.OwnsOne(t => t.Email, email =>
        {
            email.Property(e => e.Value).HasColumnName("Email").HasMaxLength(EmailAddress.MaxLength).IsRequired();
            email.HasIndex(e => e.Value).IsUnique();
        });
        builder.Navigation(t => t.Email).IsRequired();

        builder.Property(t => t.Phone).HasMaxLength(20);
        builder.Property(t => t.CreatedBy).HasMaxLength(50).IsRequired();

        // Trainer (many) -> Branch (1): the trainer's home branch.
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(t => t.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property<byte[]>(ConfigurationConstants.RowVersion).IsRowVersion();
    }
}
