using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Model.GameObjects;
using Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects.Armadyl
{
    /// <summary>
    /// </summary>
    [GameObjectScriptMetaData([26288])]
    public class ArmadylAltar : GameObjectScript
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
            switch (clickType)
            {
                // pray
                case GameObjectClickType.Option1Click:
                {
                    if (!clicker.HasState<ArmadylAltarPrayedState>())
                    {
                        if (clicker.Combat.IsInCombat())
                        {
                            return;
                        }

                        if (clicker.Statistics.HealPrayerPoints(990) != 0)
                        {
                            clicker.QueueAnimation(Animation.Create(645));
                            clicker.SendChatMessage("You prayed to the gods and they restored your prayer points.");
                            clicker.AddState(new State(StateType.ArmadylAltarPrayed, 1000));
                        }
                        else
                        {
                            clicker.SendChatMessage("You cannot restore any more prayer points.");
                        }
                    }
                    else
                    {
                        clicker.SendChatMessage("You cannot pray at this altar yet.");
                    }

                    return;
                }
                // teleport
                case GameObjectClickType.Option2Click:
                    new StandardTeleportScript(MagicBook.StandardBook, 2829, 5287, 2, 0).PerformTeleport(clicker);
                    return;
                default:
                    base.OnCharacterClickPerform(clicker, clickType);
                    break;
            }
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}