using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TitanFitness.Domain.CheckIns;

namespace TitanFitness.Infrastructure.Persistence.Configurations;

public sealed class CheckInConfiguration : IEntityTypeConfiguration<CheckIn>
{
    public void Configure(EntityTypeBuilder<CheckIn> builder)
    {
        builder.ToTable("CheckIns", table =>
            table.HasCheckConstraint(
                "CK_CheckIns_RefusalReason",
                "([Result] = 1 AND [RefusalReason] IS NULL) OR ([Result] = 2 AND [RefusalReason] IS NOT NULL)"));

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.OccurredAt).IsRequired();
        builder.Property(c => c.Result).IsRequired();
        builder.Property(c => c.RefusalReason).HasMaxLength(100);

        builder.HasIndex(c => new { c.BranchId, c.OccurredAt });
        builder.HasIndex(c => c.MemberId);
    }
}