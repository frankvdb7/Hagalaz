using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.Guilds.Fishing.GameObjects
{
    /// <summary>
    /// </summary>
    public class MainDoor : GameObjectScript
    {
        /// <summary>
        ///     Happens when character click's this object and then walks to it
        ///     and reaches it.
        ///     This method is called by OnCharacterClick by default, if OnCharacterClick is overrided
        ///     than this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character which clicked on the object.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                if (clicker.Statistics.GetSkillLevel(StatisticsConstants.Fishing) >= 68)
                {
                    if (clicker.Location.Y == 3386)
                    {
                        clicker.Movement.Teleport(Teleport.Create(Location.Create(Owner.Location.X, (short)(clicker.Location.Y + 1), Owner.Location.Z, clicker.Location.Dimension)));
                    }
                    else if (clicker.Location.Y == 3387)
                    {
                        clicker.Movement.Teleport(Teleport.Create(Location.Create(Owner.Location.X, (short)(clicker.Location.Y - 1), Owner.Location.Z, clicker.Location.Dimension)));
                    }
                }
                else
                {
                    clicker.SendChatMessage("You need a fishing level of 68 to enter the Fishing guild.");
                }
            }
            else
            {
                base.OnCharacterClickPerform(clicker, clickType);
            }
        }

        /// <summary>
        ///     Get's objectIDS which are suitable for this script.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableObjects() => [49014, 49016];

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}