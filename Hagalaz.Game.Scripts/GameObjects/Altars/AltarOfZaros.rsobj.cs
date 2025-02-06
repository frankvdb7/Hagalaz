using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.GameObjects.Altars
{
    /// <summary>
    ///     Contains altar of zaros script.
    /// </summary>
    public class AltarOfZaros : GameObjectScript
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
            if (clickType == GameObjectClickType.Option1Click)
            {
                /*if (clicker.Prayers.Book == PrayerBook.CursesBook)
                {
                    int maximum = (int)Math.Floor((double)clicker.Statistics.GetSkillLevel(Statistics.Prayer) * 1.15) * 10;
                    if (clicker.Statistics.HealPrayerPoints(maximum, maximum) > 0)
                    {
                        clicker.QueueAnimation(Animation.Create(645));
                        clicker.SendMessage("You prayed to Zaros and he restored your prayer points.");
                    }
                    else
                        clicker.SendMessage("You cannot restore any more prayer points.");
                }
                else
                {*/
                var script = clicker.ServiceProvider.GetRequiredService<SwitchPrayerBookDialogue>();
                clicker.Widgets.OpenChatboxOverlay((short)DialogueInterfaces.Send2Options, 0, script, true, Owner);
                //}
            }
            else
            {
                base.OnCharacterClickPerform(clicker, clickType);
            }
        }


        /// <summary>
        ///     Get's suitable object ids.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableObjects() => [47120];
    }
}