namespace IronMindGym.Security;

public static class SessionClaims
{
    /// <summary>The account's security stamp at sign-in. A session whose stamp no longer matches is ended.</summary>
    public const string SecurityStamp = "SecurityStamp";
}
