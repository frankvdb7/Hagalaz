using Hagalaz.Services.GameWorld.Model.Creatures;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Model.Creatures.Npcs;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CreatureTypeTests
{
    [TestMethod]
    [DataRow(typeof(Character))]
    [DataRow(typeof(Npc))]
    public void ConcreteCreatureTypes_DeriveFromCreature(Type creatureType)
    {
        Assert.AreEqual(typeof(Creature), creatureType.BaseType);
    }
}
