using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Familiars
{
    [NpcScriptMetaData([6873, 6874])]
    public class PackYak : BobFamiliarScriptBase
    {
        public PackYak(
            IItemContainerFactory itemContainerFactory, ISmartPathFinder pathFinder, INpcService npcService, IItemService itemService,
            IGroundItemBuilder groundItemBuilder) : base(itemContainerFactory, pathFinder, npcService, itemService, groundItemBuilder) { }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void InitializeBob() { }

        /// <summary>
        ///     Contains the capacity of the inventory.
        /// </summary>
        public override int InventoryCapacity => 30;

        /// <summary>
        ///     Performs the special attack.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformSpecialMove(IRuneObject target)
        {
            if (!CheckPerformSpecialMove())
            {
                SetUsingSpecialMove(false);
                return;
            }

            base.PerformSpecialMove(target);
            if (target is IItem toRemove)
            {
                Summoner.Bank.DepositFromInventory(toRemove, toRemove.Count, out var deposited);
                if (deposited != null)
                {
                    Summoner.SendChatMessage("Your " + Owner.Name + " successfully transferred " + deposited.Name + " x " + deposited.Count + " to your bank.");
                }

                Owner.QueueGraphic(Graphic.Create(1358));
            }
        }

        /// <summary>
        ///     Sets the special move target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void SetSpecialMoveTarget(IRuneObject? target)
        {
            if (target is not IItem)
            {
                return;
            }

            if (SpecialMoveClicked())
            {
                PerformSpecialMove(target);
            }
        }

        /// <summary>
        ///     Get's amount of special energy required by this FamiliarScript.
        ///     By default , this method does throw NotImplementedException
        /// </summary>
        /// <returns>
        ///     System.Int16.
        /// </returns>
        public override int GetRequiredSpecialMovePoints() => 12;

        /// <summary>
        ///     Gets the type of the special.
        /// </summary>
        /// <returns>
        ///     SpecialType
        /// </returns>
        public override FamiliarSpecialType GetSpecialType() => FamiliarSpecialType.Item;

        /// <summary>
        ///     Gets the name of the special attack.
        /// </summary>
        /// <returns>
        ///     The special attack name
        /// </returns>
        public override string GetSpecialMoveName() => "Winter Storage";

        /// <summary>
        ///     Gets the special attack description.
        /// </summary>
        /// <returns>
        ///     The special attack description
        /// </returns>
        public override string GetSpecialMoveDescription() => "Use special move on an item in your inventory to send it to your bank.";
    }
}