using IronMindGym.Security;

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
}
