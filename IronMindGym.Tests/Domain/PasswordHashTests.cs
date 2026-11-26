using IronMindGym.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace IronMindGym.Tests.Domain;

public class PasswordHashTests
{
    [Fact]
    public void PasswordsAreSaltedAndMalformedHashesAreRejected()
    {
        var first = PasswordHashUtility.Hash("TrainingPassword123!");

        Assert.NotEqual(first, PasswordHashUtility.Hash("TrainingPassword123!"));
        Assert.True(PasswordHashUtility.Verify("TrainingPassword123!", first));
        Assert.False(PasswordHashUtility.Verify("wrong", first));
        Assert.False(PasswordHashUtility.Verify("TrainingPassword123!", "invalid hash"));
    }

    [Fact]
    public void HashesMadeWithOlderSettingsAreValidButOutdated()
    {
        Assert.Equal(PasswordCheck.Valid, PasswordHashUtility.Check("TrainingPassword123!", PasswordHashUtility.Hash("TrainingPassword123!")));
        Assert.Equal(PasswordCheck.ValidButOutdated, PasswordHashUtility.Check("TrainingPassword123!", OlderHash("TrainingPassword123!")));
        Assert.Equal(PasswordCheck.Failed, PasswordHashUtility.Check("wrong", OlderHash("TrainingPassword123!")));
    }

    public static string OlderHash(string password) =>
        new PasswordHasher<object>(Options.Create(new PasswordHasherOptions { CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2 }))
            .HashPassword(new object(), password);
}
