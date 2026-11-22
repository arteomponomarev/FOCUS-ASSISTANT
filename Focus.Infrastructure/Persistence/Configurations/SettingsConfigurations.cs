using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

internal sealed class PreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("UserPreferences");
        builder.HasKey(x => x.UserId);
        builder.HasOne<User>().WithOne().HasForeignKey<UserPreferences>(x => x.UserId);
        builder.Property(x => x.AvatarId).HasMaxLength(80);
    }
}

internal sealed class WorkProfileConfiguration : IEntityTypeConfiguration<WorkProfile>
{
    public void Configure(EntityTypeBuilder<WorkProfile> builder)
    {
        builder.ToTable("WorkProfiles");
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.UserId, x.Id });
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
        builder.Property(x => x.Name).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasMany(x => x.Rules).WithOne().HasForeignKey(x => x.WorkProfileId);
        builder.Navigation(x => x.Rules).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ProcessRuleConfiguration : IEntityTypeConfiguration<ProcessRule>
{
    public void Configure(EntityTypeBuilder<ProcessRule> builder)
    {
        builder.ToTable("ProcessRules");
        builder.HasKey(x => new { x.WorkProfileId, x.ProcessName });
        builder.Property(x => x.ProcessName).HasMaxLength(200);
    }
}
