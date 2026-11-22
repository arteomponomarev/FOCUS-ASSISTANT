
using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

internal sealed class FocusStateRecordConfiguration
    : IEntityTypeConfiguration<FocusStateRecord>
{
    public void Configure(
        EntityTypeBuilder<FocusStateRecord> builder)
    {
        builder.ToTable("FocusStateRecords");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.ActivityAggregateId).IsUnique();
        builder.HasOne<ActivityAggregate>().WithOne()
            .HasForeignKey<FocusStateRecord>(x => new { x.FocusSessionId, x.ActivityAggregateId })
            .HasPrincipalKey<ActivityAggregate>(x => new { x.FocusSessionId, x.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.StartedAt)
            .IsRequired();

        builder.Property(x => x.EndedAt)
            .IsRequired();

        builder.Property(x => x.State)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.RuleVersion)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(500);

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
