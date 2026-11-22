using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

internal sealed class SessionPauseConfiguration : IEntityTypeConfiguration<SessionPause>
{
    public void Configure(EntityTypeBuilder<SessionPause> builder)
    {
        builder.ToTable("SessionPauses", table => table.HasCheckConstraint(
            "CK_SessionPauses_Interval", """ "EndedAt" IS NULL OR "EndedAt" >= "StartedAt" """));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => new { x.FocusSessionId, x.StartedAt });
        builder.HasIndex(x => x.FocusSessionId).IsUnique().HasFilter(""" "EndedAt" IS NULL """);
    }
}
