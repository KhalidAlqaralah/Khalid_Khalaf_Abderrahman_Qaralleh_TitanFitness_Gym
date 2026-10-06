using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.CheckIns;
using TitanFitness.Domain.Members;

namespace TitanFitness.Infrastructure.Persistence.Configurations;

internal sealed class CheckInConfiguration : IEntityTypeConfiguration<CheckIn>
{
    public void Configure(EntityTypeBuilder<CheckIn> builder)
    {
        builder.ToTable("CheckIns");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Notes).HasMaxLength(CheckIn.NotesMaxLength);
        builder.Property(c => c.RecordedBy).HasMaxLength(50).IsRequired();

        // CheckIn (many) -> Member (1).
        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(c => c.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // CheckIn (many) -> Branch (1).
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(c => c.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.BranchId, c.OccurredAt });
        builder.HasIndex(c => new { c.MemberId, c.OccurredAt });
    }
}
