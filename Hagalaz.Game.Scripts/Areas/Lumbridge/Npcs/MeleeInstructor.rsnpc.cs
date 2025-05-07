using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.SkillCapeDialogue;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Areas.Lumbridge.Npcs
{
    [NpcScriptMetaData([705])]
    public class MeleeInstructor : NpcScriptBase
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
                var script = clicker.ServiceProvider.GetRequiredService<SkillCapeDialogue>();
                script.SkillID = StatisticsConstants.Defence;
                clicker.Widgets.OpenDialogue(script, true, Owner);
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }
    }
}