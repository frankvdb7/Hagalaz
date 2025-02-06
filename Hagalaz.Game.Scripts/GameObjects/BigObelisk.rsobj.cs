using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;
using Hagalaz.Game.Scripts.Skills.Summoning;

namespace Hagalaz.Game.Scripts.GameObjects
{
    /// <summary>
    /// </summary>
    public class BigObelisk : GameObjectScript
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
                var pouchScreen = clicker.ServiceProvider.GetRequiredService<PouchScreen>();
                clicker.Widgets.OpenWidget(672, 0, pouchScreen, true);
            }
            else if (clickType == GameObjectClickType.Option2Click)
            {
                // TODO - Correct text and gfx.
                clicker.SendChatMessage("A magical force restored your summoning.");
                clicker.Statistics.HealSkill(StatisticsConstants.Summoning, StatisticsHelpers.LevelForExperience(StatisticsConstants.Summoning, clicker.Statistics.GetSkillExperience(StatisticsConstants.Summoning)));
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
        /// <exception cref="System.NotImplementedException"></exception>
        public override int[] GetSuitableObjects() => [28716];

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}