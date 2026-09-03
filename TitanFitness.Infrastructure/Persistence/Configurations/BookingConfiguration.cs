using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TitanFitness.Domain.Sessions;

namespace TitanFitness.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.BookedOn).IsRequired();
        builder.Property(b => b.Position).IsRequired();
        builder.Property(b => b.Status).IsRequired();
        builder.Property(b => b.Note).HasMaxLength(500);

        builder.HasIndex(b => new { b.SessionId, b.Position });
        builder.HasIndex(b => b.MemberId);
    }
}