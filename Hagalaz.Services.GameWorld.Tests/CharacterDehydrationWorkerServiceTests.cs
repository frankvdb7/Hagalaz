using AutoMapper;
using Hagalaz.Services.GameWorld.Profiles;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterDehydrationWorkerServiceTests
{
    [TestMethod]
    public void CreateRequest_GeneratesUniqueCorrelationIdsPerCharacter()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(x => x.AddProfile<CharacterProfile>())
            .BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var first = CharacterDehydrationWorkerService.CreateRequest(mapper, new CharacterModel(), 1, 10);
        var second = CharacterDehydrationWorkerService.CreateRequest(mapper, new CharacterModel(), 2, 11);

        Assert.AreNotEqual(first.CorrelationId, second.CorrelationId);
        Assert.AreEqual(1u, first.MasterId);
        Assert.AreEqual(2u, second.MasterId);
        Assert.AreEqual(10L, first.SnapshotRevision);
        Assert.AreEqual(11L, second.SnapshotRevision);
    }
}
