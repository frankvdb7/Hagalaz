using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.TzHaar.Cave.NPCs
{
    [NpcScriptMetaData([2734, 2735])]
    public class TzKih : StandardCaveNpc
    {
        public TzKih(INpcBuilder npcBuilder) : base(npcBuilder) { }

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            base.PerformAttack(target);

            if (target is ICharacter character)
            {
                character.Statistics.DrainPrayerPoints(10);
            }
        }
    }
}