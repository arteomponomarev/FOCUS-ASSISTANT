using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Accounts;

internal sealed class AccountCredential
{
    public Guid UserId { get; init; }
    public string NormalizedEmail { get; init; } = "";
    public string PasswordHash { get; init; } = "";
    public User User { get; init; } = null!;
}

internal sealed class AccountLogin
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public Guid? FamilyId { get; init; }
}

internal sealed class CredentialConfiguration : IEntityTypeConfiguration<AccountCredential>
{
    public void Configure(EntityTypeBuilder<AccountCredential> builder)
    {
        builder.ToTable("AccountCredentials");
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.NormalizedEmail).HasMaxLength(320);
        builder.Property(x => x.PasswordHash).HasMaxLength(512);
        builder.HasIndex(x => x.NormalizedEmail).IsUnique();
        builder.HasOne(x => x.User).WithOne().HasForeignKey<AccountCredential>(x => x.UserId);
    }
}

internal sealed class LoginConfiguration : IEntityTypeConfiguration<AccountLogin>
{
    public void Configure(EntityTypeBuilder<AccountLogin> builder)
    {
        builder.ToTable("AccountLogins");
        builder.HasKey(x => x.Id);
        builder.HasOne<AccountCredential>().WithMany().HasForeignKey(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.ExpiresAt });
        builder.HasIndex(x => new { x.UserId, x.FamilyId }).IsUnique();
    }
}
