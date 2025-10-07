using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Tests
{
    public class TestableCharacterCombat : CharacterCombat
    {
        public TestableCharacterCombat(ICharacter owner) : base(owner)
        {
        }

        public void SetDead(bool isDead)
        {
            IsDead = isDead;
        }

        public void SetTargetForTest(ICreature? target)
        {
            Target = target;
        }

        public void AddAttackerPublic(ICreature attacker)
        {
            AddAttacker(attacker);
        }
    }
}