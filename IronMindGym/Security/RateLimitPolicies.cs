namespace IronMindGym.Security;

public static class RateLimitPolicies
{
    /// <summary>Sign-in, registration and password changes: 10 requests per minute per IP address.</summary>
    public const string Authentication = "authentication";
}
