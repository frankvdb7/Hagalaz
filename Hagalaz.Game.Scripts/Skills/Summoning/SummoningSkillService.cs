using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Cache.Logic;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
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

        public SummoningSkillService(INpcBuilder npcBuilder) => _npcBuilder = npcBuilder;

        /// <summary>
        ///     Summons the specified FamiliarScript.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="item">The item.</param>
        public async Task SummonFamiliar(ICharacter character, IItem item)
        {
            if (!character.Area.FamiliarAllowed)
            {
                character.SendChatMessage("You cannot summon a familiar in this area.");
                return;
            }

            if (!character.EventManager.SendEvent(new SummoningAllowEvent(character)))
            {
                return;
            }

            var slot = character.Inventory.GetInstanceSlot(item);
            if (slot == -1)
            {
                return;
            }

            var summoningManager = character.ServiceProvider.GetRequiredService<ISummoningService>();
            var def = await summoningManager.FindDefinitionByPouchId(item.Id);
            if (def == null)
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

            var provider = character.ServiceProvider.GetRequiredService<IFamiliarScriptProvider>();
            var scriptType = provider.FindFamiliarScriptTypeById(def.NpcId);
            _npcBuilder
                .Create()
                .WithId(def.NpcId)
                .WithLocation(character.Location)
                .WithScript(scriptType)
                .Spawn();
            character.Inventory.Remove(item, slot);
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

                if (character.Inventory.Contains(key, amount * count))
                {
                    continue;
                }

                character.SendChatMessage("You need to have " + def.Name + " x " + amount * count);
                return false;
            }

            return true;
        }
    }
}