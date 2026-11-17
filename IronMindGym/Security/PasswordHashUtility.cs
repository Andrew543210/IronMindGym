using Microsoft.AspNetCore.Identity;

namespace IronMindGym.Security;

public static class PasswordHashUtility
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object HashingContext = new();

    public static string Hash(string password) => Hasher.HashPassword(HashingContext, password);

    public static bool Verify(string password, string hash)
    {
        try
        {
            return Hasher.VerifyHashedPassword(HashingContext, hash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
