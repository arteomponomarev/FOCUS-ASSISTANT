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

        builder.HasMany(x => x.Pauses).WithOne().HasForeignKey(x => x.FocusSessionId);
        builder.Navigation(x => x.Goals).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Pauses).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasOne<WorkProfile>().WithMany()
            .HasForeignKey(x => new { x.UserId, x.WorkProfileId })
            .HasPrincipalKey(x => new { x.UserId, x.Id })
            .OnDelete(DeleteBehavior.NoAction);
    }
}
