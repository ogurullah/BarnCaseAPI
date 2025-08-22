using System.Security.Cryptography;

namespace BarnCaseAPI.Services;

public interface IPasswordService
{
    (byte[] hash, byte[] salt) Hash(string password);
    bool Verify(string password, byte[] hash, byte[] salt);
}

public sealed class PasswordService : IPasswordService
{
    public (byte[] hash, byte[] salt) Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return (hash, salt);
    }

    public bool Verify(string password, byte[] hash, byte[] salt)
    {
        var attempt = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(hash, attempt);
    }
}