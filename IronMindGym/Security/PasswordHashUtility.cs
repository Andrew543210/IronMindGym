using Microsoft.AspNetCore.Identity;

namespace IronMindGym.Security;

public enum PasswordCheck
{
    Failed,
    Valid,
    /// <summary>The password is right, but the hash was made with older settings and should be stored again.</summary>
    ValidButOutdated
}

public static class PasswordHashUtility
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object HashingContext = new();

    // Checked when no account has the entered email, so that answer takes as long as a wrong password. It is made
    // with the hasher rather than on first use, so the first unknown email does not take twice as long.
    private static readonly string UnknownAccountHash = Hash(Guid.NewGuid().ToString("N"));

    public static string Hash(string password) => Hasher.HashPassword(HashingContext, password);

    public static PasswordCheck Check(string password, string hash)
    {
        try
        {
            return Hasher.VerifyHashedPassword(HashingContext, hash, password) switch
            {
                PasswordVerificationResult.Success => PasswordCheck.Valid,
                PasswordVerificationResult.SuccessRehashNeeded => PasswordCheck.ValidButOutdated,
                _ => PasswordCheck.Failed
            };
        }
        catch (FormatException)
        {
            return PasswordCheck.Failed;
        }
    }

    public static bool Verify(string password, string hash) => Check(password, hash) != PasswordCheck.Failed;

    /// <summary>Does the work of a password check for an email that has no account, and always fails.</summary>
    public static void CheckWithoutAccount(string password) => _ = Check(password, UnknownAccountHash);
}
