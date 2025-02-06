using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Familiars
{
    [NpcScriptMetaData([6794, 6795])]
    public class SpiritTerrorBird : BobFamiliarScriptBase
    {
        public SpiritTerrorBird(
            IItemContainerFactory itemContainerFactory, ISmartPathFinder pathFinder, INpcService npcService, IItemService itemService,
            IGroundItemBuilder groundItemBuilder) : base(itemContainerFactory, pathFinder, npcService, itemService, groundItemBuilder) { }

        /// <summary>
        ///     Initializes the bob.
        /// </summary>
        protected override void InitializeBob() { }

        /// <summary>
        ///     Contains the capacity of the inventory.
        /// </summary>
        public override int InventoryCapacity => 12;

        /// <summary>
        ///     Performs the special attack.
        /// </summary>
        public override void PerformSpecialMove(IRuneObject target)
        {
            if (!CheckPerformSpecialMove())
            {
                SetUsingSpecialMove(false);
                return;
            }

            base.PerformSpecialMove(target);
            // TODO - Hit max 80.
        }

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            if (UsingSpecialMove)
            {
                PerformSpecialMove(target);
            }
            else
            {
                base.PerformAttack(target);
            }
        }

        /// <summary>
        ///     Sets the special move target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void SetSpecialMoveTarget(IRuneObject? target)
        {
            if (target is ICreature)
            {
                if (SpecialMoveClicked())
                {
                    PerformSpecialMove(target);
                }
            }
        }

        /// <summary>
        ///     Get's if this npc can be poisoned.
        ///     By default this method checks if this npc is poisonable.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this instance can poison; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanPoison() => false;

        /// <summary>
        ///     Get's amount of special energy required by this FamiliarScript.
        ///     By default , this method does throw NotImplementedException
        /// </summary>
        /// <returns>
        ///     System.Int16.
        /// </returns>
        public override int GetRequiredSpecialMovePoints() => 8;

        /// <summary>
        ///     Gets the type of the special.
        /// </summary>
        /// <returns>
        ///     SpecialType
        /// </returns>
        public override FamiliarSpecialType GetSpecialType() => FamiliarSpecialType.Click;

        /// <summary>
        ///     Gets the name of the special attack.
        /// </summary>
        /// <returns>
        ///     The special attack name
        /// </returns>
        public override string GetSpecialMoveName() => "Tireless Run";

        /// <summary>
        ///     Gets the special attack description.
        /// </summary>
        /// <returns>
        ///     The special attack description
        /// </returns>
        public override string GetSpecialMoveDescription() => "Inflicts up to 80 damage against your opponent.";
    }
}