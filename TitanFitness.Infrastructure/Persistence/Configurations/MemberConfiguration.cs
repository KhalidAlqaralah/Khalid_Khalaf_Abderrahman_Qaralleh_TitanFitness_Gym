using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.Members;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Infrastructure.Persistence.Configurations;

internal sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.OwnsOne(m => m.Number, number =>
        {
            number.Property(n => n.Value)
                .HasColumnName("MembershipNumber")
                .HasMaxLength(MembershipNumber.MaxLength)
                .IsRequired();

            number.HasIndex(n => n.Value).IsUnique();
        });
        builder.Navigation(m => m.Number).IsRequired();

        builder.Property(m => m.FullName).HasMaxLength(Member.NameMaxLength).IsRequired();
        builder.HasIndex(m => m.FullName);

        // Optional owned value object: no email means the column is NULL.
        builder.OwnsOne(m => m.Email, email =>
            email.Property(e => e.Value).HasColumnName("Email").HasMaxLength(EmailAddress.MaxLength));

        builder.Property(m => m.Phone).HasMaxLength(20);
        builder.Property(m => m.Address).HasMaxLength(Member.AddressMaxLength);
        builder.Property(m => m.PhotoUrl).HasMaxLength(Member.PhotoUrlMaxLength);
        builder.Property(m => m.CreatedBy).HasMaxLength(50).IsRequired();

        // Member (many) -> Branch (1): the member's home branch.
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(m => m.HomeBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property<byte[]>(ConfigurationConstants.RowVersion).IsRowVersion();
    }
}
