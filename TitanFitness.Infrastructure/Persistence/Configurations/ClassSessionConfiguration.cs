using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TitanFitness.Domain.Sessions;

namespace TitanFitness.Infrastructure.Persistence.Configurations;

public sealed class ClassSessionConfiguration : IEntityTypeConfiguration<ClassSession>
{
    public void Configure(EntityTypeBuilder<ClassSession> builder)
    {
        builder.ToTable("ClassSessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
	builder.Ignore(s => s.Waitlist);

        builder.Property(s => s.ClassName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.CapacityLimit).IsRequired();
        builder.Property(s => s.Status).IsRequired();

        builder.OwnsOne(s => s.Slot, slot =>
        {
            slot.Property(x => x.Date).HasColumnName("SessionDate").IsRequired();
            slot.Property(x => x.Start).HasColumnName("StartTime").IsRequired();
            slot.Property(x => x.DurationInMinutes).HasColumnName("DurationInMinutes").IsRequired();
            slot.HasIndex(x => x.Date);
        });
        builder.Navigation(s => s.Slot).IsRequired();

        builder.HasMany(s => s.Bookings)
               .WithOne()
               .HasForeignKey(b => b.SessionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Bookings).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(s => s.TrainerId);
        builder.HasIndex(s => s.StudioId);
        builder.HasIndex(s => s.BranchId);
    }
}