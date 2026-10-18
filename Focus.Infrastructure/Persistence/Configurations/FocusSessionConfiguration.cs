using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

internal sealed class FocusSessionConfiguration
    : IEntityTypeConfiguration<FocusSession>
{
    public void Configure(
        EntityTypeBuilder<FocusSession> builder)
    {
        builder.ToTable("FocusSessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PlannedDuration)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.Reflection)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => x.StartedAt);

        builder.HasMany(x => x.Goals)
            .WithOne(x => x.FocusSession)
            .HasForeignKey(x => x.FocusSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}