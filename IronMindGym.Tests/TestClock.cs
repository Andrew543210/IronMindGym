namespace IronMindGym.Tests;

public sealed class TestClock : TimeProvider
{
    private DateTimeOffset? now;

    public override DateTimeOffset GetUtcNow() => now ?? base.GetUtcNow();

    public void Set(DateTimeOffset moment) => now = moment;
}
