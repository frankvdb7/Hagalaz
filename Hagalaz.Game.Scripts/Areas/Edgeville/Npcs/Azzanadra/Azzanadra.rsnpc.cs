using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Azzanadra
{
    [NpcScriptMetaData([9047])]
    public class Azzanadra : NpcScriptBase
    {
        public Azzanadra(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
            : base(owner, npcService, pathFinder, widgetScriptActivator)
        {
        }
        /// <summary>
        ///     Called when [character click perform].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, NpcClickType clickType)
        {
            if (clickType == NpcClickType.Option1Click)
            {
                var script = CreateWidgetScript<AzzanadraDialogue>(clicker);
                clicker.Widgets.OpenDialogue(script, true, Owner);
            }
            else
            {
                base.OnCharacterClickPerform(clicker, clickType);
            }
        }

    }
}
