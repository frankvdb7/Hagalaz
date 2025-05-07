using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Skills.Crafting;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Tanner
{
    /// <summary>
    /// </summary>
    [NpcScriptMetaData([2320])]
    public class Tanner : NpcScriptBase
    {
        private readonly ICraftingSkillService _craftingSkillService;

        public Tanner(ICraftingSkillService craftingSkillService) => _craftingSkillService = craftingSkillService;

        /// <summary>
        ///     Happens when character clicks NPC and then walks to it and reaches it.
        ///     This method is called by OnCharacterClick by default, if OnCharacter is overrided or/and
        ///     handles to click this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character that clicked this npc.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, NpcClickType clickType)
        {
            if (clickType == NpcClickType.Option3Click)
            {
                clicker.QueueTask(() => _craftingSkillService.TryTan(clicker));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}