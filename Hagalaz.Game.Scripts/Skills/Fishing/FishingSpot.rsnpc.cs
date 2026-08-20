using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Extensions;
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

        public FishingSpot(INpc owner, IFishingService fishingService, IFishingSkillService fishingSkillService, ICharacterStore characterStore,
            INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
            : base(owner, npcService, pathFinder, widgetScriptActivator)
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
        public override void OnCharacterClickPerform(ICharacter clicker, NpcClickType clickType)
        {
            clicker.QueueTask(() => StartFishingAsync(clicker, clickType));
        }

        private async Task StartFishingAsync(ICharacter clicker, NpcClickType clickType)
        {
            var interrupted = false;
            var interruptEvent = clicker.RegisterEventHandler<CreatureInterruptedEvent>(_ =>
            {
                interrupted = true;
                return false;
            });

            try
            {
                var spot = await _fishingService.FindSpotByNpcIdClickType(Owner.Appearance.CompositeID, clickType);
                var characterCount = spot is null ? 0 : await _characterStore.CountAsync();

                if (interrupted)
                {
                    return;
                }

                if (spot is null)
                {
                    base.OnCharacterClickPerform(clicker, clickType);
                    return;
                }

                if (!_fishingSkillService.TryFish(clicker, Owner, spot, characterCount))
                {
                    base.OnCharacterClickPerform(clicker, clickType);
                }
            }
            finally
            {
                clicker.UnregisterEventHandler<CreatureInterruptedEvent>(interruptEvent);
            }
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}
