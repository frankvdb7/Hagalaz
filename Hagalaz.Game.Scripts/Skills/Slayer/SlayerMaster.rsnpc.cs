using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Skills.Slayer
{
    public class SlayerMaster : NpcScriptBase
    {
        private readonly ISlayerService _slayerRepository;

        public SlayerMaster(ISlayerService slayerRepository)
        {
            _slayerRepository = slayerRepository;
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
            if (clickType == NpcClickType.Option4Click)
            {
                new OpenShopEvent(clicker, 11).Send();
            }
            else if (clickType == NpcClickType.Option3Click)
            {
                var dialogue = clicker.ServiceProvider.GetRequiredService<SlayerMasterDialogue>();
                clicker.Widgets.OpenDialogue(dialogue, true, Owner);
                dialogue.PerformDialogueOptionClick("Please give me a task.");
            }
            else if (clickType == NpcClickType.Option1Click)
            {
                var dialogue = clicker.ServiceProvider.GetRequiredService<SlayerMasterDialogue>();
                clicker.Widgets.OpenDialogue(dialogue, true, Owner);
            }
            else if (clickType == NpcClickType.Option5Click)
            {
                // rewards
                clicker.Widgets.OpenWidget(164, 0, clicker.ServiceProvider.GetRequiredService<RewardsBuyScreen>(), true);
            }
            else
            {
                base.OnCharacterClickPerform(clicker, clickType);
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