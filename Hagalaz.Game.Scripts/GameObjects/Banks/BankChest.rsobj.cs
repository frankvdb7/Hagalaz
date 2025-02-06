using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;
using Hagalaz.Game.Scripts.Widgets.Bank;

namespace Hagalaz.Game.Scripts.GameObjects.Banks
{
    /// <summary>
    ///     Contains bank booth script.
    /// </summary>
    [GameObjectScriptMetaData([4483, 8981, 20607, 27663, 42192])]
    public class BankChest : GameObjectScript
    {
        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize()
        {
        }

        /// <summary>
        ///     Happens on character click.
        /// </summary>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click || clickType == GameObjectClickType.Option2Click)
            {
                var bankScript = clicker.ServiceProvider.GetRequiredService<BankScreen>();
                clicker.Widgets.OpenWidget(762, 0, bankScript, true);
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }
    }
}