using System.Security.Cryptography;
using Focus.Application.Accounts;
using Focus.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Focus.Infrastructure.Accounts;

internal sealed class AccountPassword : IAccountPassword
{
    private readonly PasswordHasher<User> _hasher = new();
    private readonly User _dummy = new("unused@example.invalid", "Unused");
    private readonly string _dummyHash;

    public AccountPassword()
    {
        _dummyHash = _hasher.HashPassword(_dummy, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    }

    public string Hash(User user, string password) => _hasher.HashPassword(user, password);

    public bool Verify(Account? account, string password) =>
        _hasher.VerifyHashedPassword(account?.User ?? _dummy, account?.PasswordHash ?? _dummyHash, password)
            != PasswordVerificationResult.Failed && account is not null;
}
