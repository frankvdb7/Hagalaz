using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Thurgo
{
    /// <summary>
    ///     Contains Thurgo script.
    /// </summary>
    [NpcScriptMetaData([604])]
    public class Thurgo : NpcScriptBase
    {
        /// <summary>
        ///     Called when [character click perform].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, NpcClickType clickType)
        {
            if (clickType == NpcClickType.Option1Click)
            {
                var skillCapeDialogue = clicker.ServiceProvider.GetRequiredService<SkillCapeDialogue.SkillCapeDialogue>();
                skillCapeDialogue.SkillID = StatisticsConstants.Smithing;
                clicker.Widgets.OpenDialogue(skillCapeDialogue, true, Owner);
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }
    }
}