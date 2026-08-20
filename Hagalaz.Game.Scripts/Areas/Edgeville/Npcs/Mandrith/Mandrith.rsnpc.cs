using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Mandrith
{
    [NpcScriptMetaData([6537])]
    public class Mandrith : NpcScriptBase
    {
        public Mandrith(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
            : base(owner, npcService, pathFinder, widgetScriptActivator)
        {
        }
        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        protected override void Initialize()
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
                var dialogue = CreateWidgetScript<MandrithDialogue>(clicker);
                clicker.Widgets.OpenDialogue(dialogue, true, Owner);
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }
    }
}
