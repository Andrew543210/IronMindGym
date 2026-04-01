using System.Security.Cryptography;
using System.Text;

namespace IronMindGym.Security;

public static class PasswordHashUtility
{
    public static string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public static bool Verify(string password, string hash)
    {
        return string.Equals(Hash(password), hash, StringComparison.OrdinalIgnoreCase);
    }
}

