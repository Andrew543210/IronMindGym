namespace IronMindGym.Tests;

/// <summary>The application's clock in tests: the real time until a test sets a moment, which then stays fixed.</summary>
public sealed class TestClock : TimeProvider
{
    private DateTimeOffset? now;

    public override DateTimeOffset GetUtcNow() => now ?? base.GetUtcNow();

    public void Set(DateTimeOffset moment) => now = moment;
}
