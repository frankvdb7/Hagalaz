using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;
using Hagalaz.Game.Scripts.Widgets.Bank;

namespace Hagalaz.Game.Scripts.GameObjects.Banks
{
    /// <summary>
    ///     Contains bank booth script.
    /// </summary>
    [GameObjectScriptMetaData([
        // bank both ids
        782, 2012, 2015, 2019, 2213, 2214, 3045, 5276, 6084, 10517, 11338, 11402, 11758, 12798, 12799, 12800, 12801, 14369, 14370, 16700, 18491, 19230, 20325,
        20327, 20328, 22819, 24914, 25808, 26972, 29085, 34205, 34752, 35647, 35648, 36262, 36786, 37474, 49018, 49019, 52397, 52398, 66665, 66666, 66667,
        69021, 69022, 69023, 69024,
        // bank counter ids
        42217, 42378, 42377
    ])]
    public class BankBooth : GameObjectScript
    {
        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize()
        {
            //World.RegionManager.FlagCollision(this.owner.Location, CollisionFlag.ObjectTile);
            //World.RegionManager.FlagCollision(this.owner.Location, CollisionFlag.ObjectBlock);
            //World.RegionManager.FlagCollision(this.owner.Location, CollisionFlag.ObjectAllowRange);
        }

        /// <summary>
        ///     Happens on character click.
        /// </summary>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option2Click || clickType == GameObjectClickType.Option1Click)
            {
                var bankScript = clicker.ServiceProvider.GetRequiredService<BankScreen>();
                clicker.Widgets.OpenWidget(762, 0, bankScript, true);
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }
    }
}