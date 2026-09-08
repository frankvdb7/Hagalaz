using System;
using System.Collections.Generic;
using AutoMapper;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using NSubstitute;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class MapRegionClientUpdateTests
{
    [TestMethod]
    public void MajorClientUpdateTick_ContinuesAfterOneCharacterUpdateFails()
    {
        var mapper = Substitute.For<IMapper>();
        mapper.Map<RaidoMessage>(Arg.Any<object>()).Returns(Substitute.For<RaidoMessage>());
        var region = new MapRegion(
            Location.Zero,
            new int[4],
            Substitute.For<INpcService>(),
            Substitute.For<IMapRegionService>(),
            Substitute.For<IGameObjectBuilder>(),
            Substitute.For<IGroundItemBuilder>(),
            mapper);
        var failingCharacter = CreateCharacter(1);
        var healthyCharacter = CreateCharacter(2);
        failingCharacter.Session.When(session => session.SendMessage(Arg.Any<RaidoMessage>()))
            .Do(_ => throw new InvalidOperationException("character update failed"));

        region.Add(failingCharacter);
        region.Add(healthyCharacter);
        region.QueueUpdate(new TestRegionPartUpdate());
        region.MajorClientPrepareUpdateTick();

        Assert.ThrowsExactly<AggregateException>(() =>
            region.MajorClientUpdateTick(new Dictionary<int, ICharacter>()));

        healthyCharacter.Session.Received().SendMessage(Arg.Any<RaidoMessage>());

        region.MajorClientUpdateResetTick();
    }

    private static ICharacter CreateCharacter(int index)
    {
        var character = Substitute.For<ICharacter>();
        character.Index.Returns(index);
        character.Viewport.Returns(Substitute.For<IViewport>());
        character.Session.Returns(Substitute.For<IGameSession>());
        return character;
    }

    private sealed class TestRegionPartUpdate : IRegionPartUpdate
    {
        public ILocation Location { get; } = Hagalaz.Game.Abstractions.Model.Location.Zero;

        public bool CanUpdateFor(ICharacter character) => true;

        public void OnUpdatedFor(ICharacter character) { }
    }
}
