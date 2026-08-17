using Hagalaz.Services.GameWorld.Services;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class WorldLifecycleStateTests
{
    [TestMethod]
    public void ReadyStateRequiresInitializationAndRegistration()
    {
        var state = new WorldLifecycleState();
        state.MarkApplicationStarted();
        state.MarkCompleted();

        Assert.IsFalse(state.CanAcceptWorldSignIns);

        state.MarkRegistrationSucceeded();

        Assert.IsTrue(state.CanAcceptWorldSignIns);
    }

    [TestMethod]
    public void StoppingRemovesReadinessAndSignInAdmission()
    {
        var state = new WorldLifecycleState();
        state.MarkApplicationStarted();
        state.MarkCompleted();
        state.MarkRegistrationSucceeded();

        state.MarkStopping();

        Assert.IsTrue(state.IsStopping);
        Assert.IsFalse(state.CanAcceptWorldSignIns);
        Assert.IsFalse(state.IsRegistrationHealthy);
    }
}
