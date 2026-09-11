using System.Security.Cryptography;
using Sabat.Application.Abstractions;

namespace Sabat.Infrastructure.Security;

public sealed class Pbkdf2AdminPasswordHasher : IAdminPasswordHasher
{
    private const string Algorithm = "pbkdf2-sha512";
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA512,
            HashSize);

        return string.Join(
            '$',
            Algorithm,
            Iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string encodedHash)
    {
        try
        {
            var parts = encodedHash.Split('$', StringSplitOptions.None);
            if (parts.Length != 4 || parts[0] != Algorithm ||
                !int.TryParse(parts[1], out var iterations) ||
                iterations is < 100_000 or > 1_000_000)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            if (salt.Length < SaltSize || expectedHash.Length != HashSize)
            {
                return false;
            }

            var suppliedHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA512,
                expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(suppliedHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
