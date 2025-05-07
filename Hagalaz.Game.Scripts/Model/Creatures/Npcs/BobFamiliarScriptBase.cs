using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Logic.Characters.Model;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Skills.Summoning;

namespace Hagalaz.Game.Scripts.Model.Creatures.Npcs
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class BobFamiliarScriptBase : FamiliarScriptBase, IHydratable<IReadOnlyList<HydratedItem>>, IDehydratable<IReadOnlyList<HydratedItem>>
    {
        private readonly IGroundItemBuilder _groundItemBuilder;

        /// <summary>
        /// Contains the inventory.
        /// </summary>
        public IFamiliarInventoryContainer Inventory { get; private set; }

        /// <summary>
        /// Contains the capacity of the inventory.
        /// </summary>
        public abstract int InventoryCapacity { get; }

        public BobFamiliarScriptBase(
            IItemContainerFactory itemContainerFactory, ISmartPathFinder pathFinder, INpcService npcService, IItemService itemService,
            IGroundItemBuilder groundItemBuilder)
            : base(pathFinder, npcService, itemService)
        {
            _groundItemBuilder = groundItemBuilder;
            Inventory = itemContainerFactory.Create(Summoner, StorageType.Normal, InventoryCapacity);
        }

        /// <summary>
        /// Initializes the familiar.
        /// </summary>
        protected sealed override void InitializeFamiliar() => InitializeBob();

        /// <summary>
        /// Initializes the beast of burden.
        /// </summary>
        protected abstract void InitializeBob();

        /// <summary>
        /// Get's called when npc is killed.
        /// </summary>
        /// <param name="killer"></param>
        public override void OnKilledBy(ICreature killer) => DropAllItems(killer);

        /// <summary>
        /// Drops all items.
        /// </summary>
        /// <param name="newOwner">The new owner, can be null.</param>
        private void DropAllItems(ICreature newOwner)
        {
            for (var i = 0; i < Inventory.Capacity; i++)
            {
                var it = Inventory[i];
                if (it == null)
                {
                    continue;
                }

                var tradeable = it.ItemScript.CanTradeItem(it, Summoner);
                if (tradeable || newOwner == null || newOwner is INpc) // only allow tradeable, killer == null and NPCs
                {
                    var groundItemOwner = Summoner;
                    if (newOwner != null)
                    {
                        if (newOwner is INpc npc)
                        {
                            if (npc.TryGetScript<FamiliarScriptBase>(out var script)) groundItemOwner = script.Summoner;
                        }
                        else if (newOwner is ICharacter character) groundItemOwner = character;
                    }

                    _groundItemBuilder.Create()
                        .WithItem(it)
                        .WithLocation(Owner.Location)
                        .WithOwner(groundItemOwner)
                        .Spawn();
                }

                Inventory.Remove(it, i, false);
            }
        }

        /// <summary>
        /// Called when [use item].
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="used">The used.</param>
        /// <returns></returns>
        public override bool UseItemOnNpc(IItem used, ICharacter character)
        {
            if (character != Summoner) return false;
            return Inventory.DepositFromInventory(used, used.Count);
        }

        /// <summary>
        /// Happens when character clicks NPC and then walks to it and reaches it.
        /// This method is called by OnCharacterClick by default, if OnCharacter is overrided or/and
        /// handles to click this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character that clicked this npc.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, NpcClickType clickType)
        {
            if (clickType == NpcClickType.Option3Click && Summoner == clicker)
            {
                var script = clicker.ServiceProvider.GetRequiredService<FamiliarInventoryWidget>();
                clicker.Widgets.OpenWidget(671, 0, script, true);
            }
            else
                base.OnCharacterClickPerform(clicker, clickType);
        }

        public void Hydrate(IReadOnlyList<HydratedItem> hydration)
        {
            if (Inventory is IHydratable<IReadOnlyList<HydratedItem>> hydratable)
            {
                hydratable.Hydrate(hydration);
            }
        }

        IReadOnlyList<HydratedItem> IDehydratable<IReadOnlyList<HydratedItem>>.Dehydrate()
        {
            if (Inventory is IDehydratable<IReadOnlyList<HydratedItem>> dehydratable)
            {
                return dehydratable.Dehydrate();
            }

            return new List<HydratedItem>();
        }
    }
}