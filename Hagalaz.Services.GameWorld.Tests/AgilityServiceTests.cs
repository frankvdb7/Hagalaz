using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Services;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class AgilityServiceTests
{
    [TestMethod]
    public void Lookups_UseThePreloadedModelAndNeverTouchTheServiceProvider()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var service = new AgilityService(serviceProvider);

        Assert.IsNull(service.FindObstacleByObjectId(69526));
        Assert.IsNull(service.FindCourseById(AgilityCourseId.GnomeBasic));
        serviceProvider.ReceivedCalls().ToList().ForEach(call => Assert.Fail($"Unexpected provider call {call.GetMethodInfo().Name}"));
    }
}
