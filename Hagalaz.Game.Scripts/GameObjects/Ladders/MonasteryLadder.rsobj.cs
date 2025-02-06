using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Scripts.GameObjects.Ladders
{
    /// <summary>
    /// </summary>
    public class MonasteryLadder : LadderObjectScript
    {
        /// <summary>
        ///     Happens on character click.
        /// </summary>
        /// <param name="clicker"></param>
        /// <param name="clickType"></param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                if (clicker.Statistics.GetSkillLevel(StatisticsConstants.Prayer) < 31)
                {
                    clicker.SendChatMessage("You need a prayer level of 31 in order to climb up.");
                    return;
                }
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Get's suitable object ids.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableObjects() =>
        [
            2641, 30863 //monastery
        ];
    }
}