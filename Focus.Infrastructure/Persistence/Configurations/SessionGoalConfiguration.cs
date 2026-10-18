using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

internal sealed class SessionGoalConfiguration
    : IEntityTypeConfiguration<SessionGoal>
{
    public void Configure(
        EntityTypeBuilder<SessionGoal> builder)
    {
        builder.ToTable("SessionGoals");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.HasIndex(x => x.FocusSessionId);
    }
}