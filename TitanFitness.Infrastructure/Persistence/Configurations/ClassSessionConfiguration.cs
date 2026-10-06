using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.Members;
using TitanFitness.Domain.Sessions;
using TitanFitness.Domain.Trainers;

namespace TitanFitness.Infrastructure.Persistence.Configurations;

internal sealed class ClassSessionConfiguration : IEntityTypeConfiguration<ClassSession>
{
    public void Configure(EntityTypeBuilder<ClassSession> builder)
    {
        builder.ToTable("ClassSessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.ClassName).HasMaxLength(ClassSession.NameMaxLength).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(ClassSession.DescriptionMaxLength);
        builder.Property(s => s.CreatedBy).HasMaxLength(50).IsRequired();

        builder.ComplexProperty(s => s.Slot, slot =>
        {
            slot.Property(x => x.Date).HasColumnName("Date");
            slot.Property(x => x.Start).HasColumnName("StartTime");
            slot.Property(x => x.DurationInMinutes).HasColumnName("DurationInMinutes");
        });

        // ClassSession (many) -> Branch (1).
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        // ClassSession (many) -> Trainer (0..1): optional instructor.
        builder.HasOne<Trainer>()
            .WithMany()
            .HasForeignKey(s => s.TrainerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // ClassSession (many) -> Studio (0..1): optional room.
        builder.HasOne<Studio>()
            .WithMany()
            .HasForeignKey(s => s.StudioId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // ClassSession (1) -> Bookings (many): children of the aggregate.
        builder.HasMany(s => s.Bookings)
            .WithOne()
            .HasForeignKey(b => b.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Bookings).HasField("_bookings").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();

        builder.Ignore(s => s.Waitlist);

        builder.Property<byte[]>(ConfigurationConstants.RowVersion).IsRowVersion();
    }
}

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Note).HasMaxLength(Booking.NoteMaxLength);

        // Booking (many) -> Member (1).
        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(b => b.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => new { b.SessionId, b.MemberId });
    }
}
