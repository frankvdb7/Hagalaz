using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Cache.Logic;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Common.Events.Character;

namespace Hagalaz.Game.Scripts.Skills.Summoning
{
    /// <summary>
    ///     Static summoning unEquipHandler class for the Summoning skill.
    /// </summary>
    public class SummoningSkillService : ISummoningSkillService
    {
        private readonly INpcBuilder _npcBuilder;
        private readonly IFamiliarScriptProvider _familiarScriptProvider;
        private readonly IItemBuilder _itemBuilder;

        /// <summary>
        ///     The amount of scrolls per pouch.
        /// </summary>
        private const int _scrollsPerPouch = 10;

        public SummoningSkillService(INpcBuilder npcBuilder, IFamiliarScriptProvider familiarScriptProvider, IItemBuilder itemBuilder)
        {
            _npcBuilder = npcBuilder;
            _familiarScriptProvider = familiarScriptProvider;
            _itemBuilder = itemBuilder;
        }

        /// <summary>
        ///     Infuses pouches atomically: the ingredients are consumed, the pouches inserted and the experience granted
        ///     together, or nothing changes.
        /// </summary>
        public bool TryCreatePouches(ICharacter character, SummoningDto definition, int itemId, int count)
        {
            if (count <= 0 || !CanCreatePouch(character, definition, itemId, count))
            {
                return false;
            }

            var itemDef = character.ServiceProvider.GetRequiredService<IItemService>().FindItemDefinitionById(itemId);
            var requirements = ItemTypeLogic.GetCreateItemRequirements(itemDef);
            if (requirements == null || requirements.Count == 0)
            {
                character.SendChatMessage("You don't have the required items to create this pouch.");
                return false;
            }

            using (var transaction = ItemContainerTransaction.Begin(character.Inventory))
            {
                foreach (var (key, amount) in requirements)
                {
                    if (!TryMultiply(amount, count, out var total) ||
                        !character.Inventory.TryRemoveItemExact(_itemBuilder.Create().WithId(key).WithCount(total).Build()))
                    {
                        character.SendChatMessage("You don't have the required items to create this pouch.");
                        return false;
                    }
                }

                if (!character.Inventory.AddItem(_itemBuilder.Create().WithId(itemId).WithCount(count).Build()))
                {
                    character.SendChatMessage("You don't have enough inventory space.");
                    return false;
                }

                transaction.Commit();
            }

            character.Statistics.AddExperience(StatisticsConstants.Summoning, definition.CreatePouchExperience * count);
            return true;
        }

        /// <summary>
        ///     Transforms up to <paramref name="count" /> pouches into scrolls atomically; nothing changes and no
        ///     experience is granted when the scrolls cannot be inserted.
        /// </summary>
        public bool TryCreateScrolls(ICharacter character, SummoningDto definition, int count)
        {
            if (count <= 0 || !CanCreateScroll(character, definition))
            {
                return false;
            }

            var pouches = Math.Min(count, character.Inventory.Items.GetCountById(definition.PouchId));
            if (pouches <= 0 || !TryMultiply(pouches, _scrollsPerPouch, out var scrolls))
            {
                character.SendChatMessage("You don't have the required pouch to create this scroll.");
                return false;
            }

            using (var transaction = ItemContainerTransaction.Begin(character.Inventory))
            {
                if (!character.Inventory.TryRemoveItemExact(_itemBuilder.Create().WithId(definition.PouchId).WithCount(pouches).Build()))
                {
                    return false;
                }

                if (!character.Inventory.AddItem(_itemBuilder.Create().WithId(definition.ScrollId).WithCount(scrolls).Build()))
                {
                    character.SendChatMessage("You don't have enough inventory space.");
                    return false;
                }

                transaction.Commit();
            }

            character.Statistics.AddExperience(StatisticsConstants.Summoning, definition.ScrollExperience * pouches);
            return true;
        }

        private static bool TryMultiply(int a, int b, out int result)
        {
            var product = (long)a * b;
            result = (int)product;
            return product is > 0 and <= int.MaxValue;
        }

        /// <summary>
        ///     Summons the specified FamiliarScript.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">Cancels the action.</param>
        public async Task SummonFamiliar(ICharacter character, IItem item, System.Threading.CancellationToken cancellationToken = default)
        {
            var pouchId = item.Id;
            var summoningManager = character.ServiceProvider.GetRequiredService<ISummoningService>();
            var def = await summoningManager.FindDefinitionByPouchId(pouchId);
            cancellationToken.ThrowIfCancellationRequested();
            if (def == null)
            {
                return;
            }

            if (!character.Area.FamiliarAllowed)
            {
                character.SendChatMessage("You cannot summon a familiar in this area.");
                return;
            }

            if (!character.EventManager.SendEvent(new SummoningAllowEvent(character)))
            {
                return;
            }

            var slot = character.Inventory.Items.GetInstanceSlot(item);
            if (slot == -1)
            {
                return;
            }

            if (character.Statistics.LevelForExperience(StatisticsConstants.Summoning) < def.SummonLevel)
            {
                character.SendChatMessage("You need a summoning level of " + def.SummonLevel + " to summon this familiar.");
                return;
            }

            if (character.Statistics.GetSkillLevel(StatisticsConstants.Summoning) < def.SummonSpawnCost)
            {
                character.SendChatMessage("You need to recharge your Summoning at an obelisk.");
                return;
            }

            var scriptType = _familiarScriptProvider.FindFamiliarScriptTypeById(def.NpcId);
            _npcBuilder
                .Create()
                .WithId(def.NpcId)
                .WithLocation(character.Location)
                .WithScript(scriptType, script =>
                {
                    var familiarScript = (IFamiliarScript)script;
                    familiarScript.AttachToSummoner(character, def);
                    character.AttachFamiliar(familiarScript);
                })
                .Spawn();
            character.Inventory.RemoveItem(item, slot);
            character.Statistics.DamageSkill(StatisticsConstants.Summoning, def.SummonSpawnCost);
            character.Statistics.AddExperience(StatisticsConstants.Summoning, def.SummonExperience);
        }

        /// <summary>
        ///     Determines whether this instance [can create scroll] the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="definition">The definition.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can create scroll] the specified character; otherwise, <c>false</c>.
        /// </returns>
        public bool CanCreateScroll(ICharacter character, SummoningDto definition)
        {
            if (character.Statistics.GetSkillLevel(StatisticsConstants.Summoning) >= definition.SummonLevel)
            {
                return true;
            }

            character.SendChatMessage("You need a summoning level of " + definition.SummonLevel + " to create this scroll.");
            return false;

        }

        /// <summary>
        ///     Determines whether this instance [can create pouch] the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="count">The count.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can create pouch] the specified character; otherwise, <c>false</c>.
        /// </returns>
        public bool CanCreatePouch(ICharacter character, SummoningDto definition, int itemId, int count)
        {
            if (character.Statistics.GetSkillLevel(StatisticsConstants.Summoning) < definition.SummonLevel)
            {
                character.SendChatMessage("You need a summoning level of " + definition.SummonLevel + " to create this pouch.");
                return false;
            }

            var itemRepository = character.ServiceProvider.GetRequiredService<IItemService>();
            var itemDef = itemRepository.FindItemDefinitionById(itemId);
            var requirements = ItemTypeLogic.GetCreateItemRequirements(itemDef);
            if (requirements == null)
            {
                return true;
            }

            foreach (var (key, amount) in requirements)
            {
                var def = itemRepository.FindItemDefinitionById(key);

                if (TryMultiply(amount, count, out var total) && character.Inventory.Items.Contains(key, total))
                {
                    continue;
                }

                character.SendChatMessage("You need to have " + def.Name + " x " + (long)amount * count);
                return false;
            }

            return true;
        }
    }
}
