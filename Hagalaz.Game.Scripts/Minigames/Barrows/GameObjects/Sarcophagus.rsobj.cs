using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Minigames.Barrows.GameObjects
{
    /// <summary>
    /// </summary>
    [GameObjectScriptMetaData([61189, 63177, 66016, 66017, 66018, 66019, 66020])]
    public class Sarcophagus : GameObjectScript
    {
        /// <summary>
        ///     Called when [character click perform].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                if (!clicker.HasScript<BarrowsScript>())
                {
                    return;
                }

                var id = Owner.Id;
                var b = clicker.GetOrAddScript<BarrowsScript>();
                if (id == 66017) // ahrim
                {
                    b.SpawnBarrowBrother(BarrowsConstants.BarrowBrotherIDs[0]);
                }
                else if (id == 63177) // dharok
                {
                    b.SpawnBarrowBrother(BarrowsConstants.BarrowBrotherIDs[1]);
                }
                else if (id == 66020) // guthan
                {
                    b.SpawnBarrowBrother(BarrowsConstants.BarrowBrotherIDs[2]);
                }
                else if (id == 66018) // karil
                {
                    b.SpawnBarrowBrother(BarrowsConstants.BarrowBrotherIDs[3]);
                }
                else if (id == 66019) // torag
                {
                    b.SpawnBarrowBrother(BarrowsConstants.BarrowBrotherIDs[4]);
                }
                else if (id == 66016) // verac
                {
                    b.SpawnBarrowBrother(BarrowsConstants.BarrowBrotherIDs[5]);
                }
                else if (id == 61189) // akrisae
                {
                    b.SpawnBarrowBrother(BarrowsConstants.BarrowBrotherIDs[6]);
                }
            }
            else
            {
                base.OnCharacterClickPerform(clicker, clickType);
            }
        }
    }
}