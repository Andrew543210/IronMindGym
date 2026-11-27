using IronMindGym.Data;
using Microsoft.Extensions.DependencyInjection;

namespace IronMindGym.Tests.Data;

public class WriteLockTests
{
    [Fact]
    public async Task ARequestCancelledWhileWaitingForTheLockLeavesNoTransactionBehind()
    {
        using var app = new TestApplication();
        using var holderScope = app.Services.CreateScope();
        using var waiterScope = app.Services.CreateScope();
        var holder = holderScope.ServiceProvider.GetRequiredService<IronMindDbContext>();
        var waiter = waiterScope.ServiceProvider.GetRequiredService<IronMindDbContext>();
        await using var held = await WriteLock.BeginForAthleteAsync(holder, 7, CancellationToken.None);
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WriteLock.BeginForAthleteAsync(waiter, 7, cancel.Token));

        Assert.Null(waiter.Database.CurrentTransaction);
    }
}
