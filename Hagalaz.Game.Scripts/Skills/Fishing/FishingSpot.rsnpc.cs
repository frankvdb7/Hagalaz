using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Skills.Fishing
{
    /// <summary>
    /// </summary>
    /// <seealso cref="NpcScriptBase" />
    public class FishingSpot : NpcScriptBase
    {
        private readonly IFishingService _fishingService;
        private readonly IFishingSkillService _fishingSkillService;
        private readonly ICharacterStore _characterStore;

        public FishingSpot(IFishingService fishingService, IFishingSkillService fishingSkillService, ICharacterStore characterStore)
        {
            _fishingService = fishingService;
            _fishingSkillService = fishingSkillService;
            _characterStore = characterStore;
        }

        /// <summary>
        ///     Happens when character clicks NPC and then walks to it and reaches it.
        ///     This method is called by OnCharacterClick by default, if OnCharacter is overrided or/and
        ///     handles to click this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character that clicked this npc.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, NpcClickType clickType) =>
            clicker.QueueAsyncTask(async () =>
            {
                var spot = await _fishingService.FindSpotByNpcIdClickType(Owner.Appearance.CompositeID, clickType);
                if (spot == null)
                {
                    return () => base.OnCharacterClickPerform(clicker, clickType);
                }

                var characterCount = await _characterStore.CountAsync();
                return () =>
                {
                    if (_fishingSkillService.TryFish(clicker, Owner, spot, characterCount))
                    {
                        return;
                    }

                    base.OnCharacterClickPerform(clicker, clickType);
                };
            });

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}
