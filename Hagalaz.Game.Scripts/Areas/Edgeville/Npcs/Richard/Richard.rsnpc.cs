using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Richard
{
    /// <summary>
    /// </summary>
    [NpcScriptMetaData([1783])]
    public class Richard : NpcScriptBase
    {
        /// <summary>
        ///     Called when [character click perform].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, NpcClickType clickType)
        {
            if (clickType == NpcClickType.Option3Click)
            {
                clicker.EventManager.SendEvent(new OpenShopEvent(clicker, 26));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}