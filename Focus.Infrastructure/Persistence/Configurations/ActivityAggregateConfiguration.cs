
using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Focus.Infrastructure.Persistence.Configurations;

internal sealed class ActivityAggregateConfiguration
    : IEntityTypeConfiguration<ActivityAggregate>
{
    public void Configure(
        EntityTypeBuilder<ActivityAggregate> builder)
    {
        builder.ToTable("ActivityAggregates", table => table.HasCheckConstraint("CK_ActivityAggregates_CameraOnly",
            """ "Vision" IS NULL OR (jsonb_typeof("Vision") = 'object' AND "ActiveProcessName" IS NULL AND "KeyboardEventCount" = 0 AND "MouseClickCount" = 0 AND "WindowSwitchCount" = 0 AND "IdleSeconds" = 0) """));
        builder.Property(x => x.Vision).HasColumnType("jsonb").HasConversion(
            x => JsonSerializer.Serialize(x, (JsonSerializerOptions?)null),
            x => JsonSerializer.Deserialize<VisionAggregate>(x, (JsonSerializerOptions?)null));

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.FocusSessionId, x.ClientEventId }).IsUnique();
        builder.HasAlternateKey(x => new { x.FocusSessionId, x.Id });

        builder.Property(x => x.StartedAt)
            .IsRequired();

        builder.Property(x => x.EndedAt)
            .IsRequired();

        builder.Property(x => x.ActiveProcessName)
            .HasMaxLength(200);

        builder.Property(x => x.KeyboardEventCount)
            .IsRequired();

        builder.Property(x => x.MouseClickCount)
            .IsRequired();

        builder.Property(x => x.WindowSwitchCount)
            .IsRequired();

        builder.Property(x => x.IdleSeconds)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.FocusSessionId,
            x.StartedAt
        });

        builder.HasOne(x => x.FocusSession)
            .WithMany()
            .HasForeignKey(x => x.FocusSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
