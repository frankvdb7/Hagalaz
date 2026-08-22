using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Characters;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Characters;

[TestClass]
public sealed class FamiliarCharacterScriptTests
{
    [TestMethod]
    public void OnRegistered_RestoresHydratedFamiliarThroughNpcBuilder()
    {
        var character = Substitute.For<ICharacter>();
        var familiar = Substitute.For<IFamiliarScript>();
        var location = Substitute.For<ILocation>();
        var context = Substitute.For<ICharacterContext>();
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        var builder = Substitute.For<INpcBuilder>();
        var npcId = Substitute.For<INpcId>();
        var npcLocation = Substitute.For<INpcLocation>();
        var optional = Substitute.For<INpcOptional>();

        familiar.FamiliarId.Returns(6815);
        character.FamiliarScript.Returns(familiar);
        character.Location.Returns(location);
        context.Character.Returns(character);
        contextAccessor.Context.Returns(context);
        builder.Create().Returns(npcId);
        npcId.WithId(6815).Returns(npcLocation);
        npcLocation.WithLocation(location).Returns(optional);
        optional.WithScript(familiar).Returns(optional);

        new FamiliarCharacterScript(contextAccessor, builder).OnRegistered();

        optional.Received(1).WithScript(familiar);
        optional.Received(1).Spawn();
    }
}
