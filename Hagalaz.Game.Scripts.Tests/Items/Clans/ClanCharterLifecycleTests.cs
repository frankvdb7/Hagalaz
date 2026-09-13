using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Scripts.Items.Clans;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Items.Clans;

[TestClass]
public sealed class ClanCharterLifecycleTests
{
    [TestMethod]
    public void ClanFounding_RemovesFounderWhenDestroyEventIsRaised()
    {
        var owner = Substitute.For<ICharacter>();
        var founder = Substitute.For<ICharacter>();
        owner.DisplayName.Returns("Owner");
        founder.DisplayName.Returns("Founder");
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(owner);
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        contextAccessor.Context.Returns(context);
        EventHappened<CreatureDestroyedEvent>? destroyHandler = null;
        founder.RegisterEventHandler<CreatureDestroyedEvent>(Arg.Do<EventHappened<CreatureDestroyedEvent>>(handler => destroyHandler = handler))
            .Returns((EventHappened)(_ => false));

        var script = new ClanCharter.ClanFounding(contextAccessor);
        script.AddFounder(founder);

        Assert.AreEqual(2, script.GetFounderCount());
        Assert.IsNotNull(destroyHandler);

        destroyHandler!(new CreatureDestroyedEvent(founder));

        Assert.AreEqual(1, script.GetFounderCount());
        Assert.IsFalse(script.Founders.Contains(founder));
    }
}
