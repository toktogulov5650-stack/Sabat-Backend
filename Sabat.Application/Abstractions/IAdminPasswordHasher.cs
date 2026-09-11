namespace Sabat.Application.Abstractions;

public interface IAdminPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string encodedHash);
}
