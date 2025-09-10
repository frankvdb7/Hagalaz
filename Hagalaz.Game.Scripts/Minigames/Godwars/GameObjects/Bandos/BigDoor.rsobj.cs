using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects.Bandos
{
    /// <summary>
    ///     Contains bandos big door script.
    /// </summary>
    [GameObjectScriptMetaData([26384, 26425])]
    public class BigDoor : GameObjectScript
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
                clicker.Interrupt(this);
                switch (Owner.Id)
                {
                    case 26384 when !clicker.Inventory.Contains(2347):
                        clicker.SendChatMessage("You need a hammer to bang the door.");
                        return;
                    case 26384 when clicker.Statistics.GetSkillLevel(StatisticsConstants.Strength) < 70:
                        clicker.SendChatMessage("You need a strength level of 70 to bang the door.");
                        return;
                    case 26384:
                        clicker.Movement.Lock(true);
                        clicker.QueueAnimation(Animation.Create(7002));
                        clicker.QueueTask(new RsTask(() =>
                            {
                                clicker.Movement.Teleport(Teleport.Create(Location.Create(clicker.Location.X == 2851 ? 2850 : 2851, clicker.Location.Y, clicker.Location.Z, 0)));
                                clicker.Movement.Unlock(false);
                            }, 2));
                        return;
                    case 26425:
                    {
                        if (clicker.Location.X == 2863)
                        {
                            var script = clicker.GetScript<GodwarsScript>();
                            if (script == null)
                            {
                                return;
                            }

                            if (script.BandosKills < 20)
                            {
                                clicker.SendChatMessage("You need 20 Bandos kills to pass through this door.");
                                return;
                            }

                            script.ResetBandosKills();
                        }

                        clicker.Movement.Lock(true);
                        clicker.QueueTask(new RsTask(() =>
                            {
                                clicker.Movement.Teleport(Teleport.Create(Location.Create(clicker.Location.X == 2863 ? 2864 : 2863, clicker.Location.Y, clicker.Location.Z, 0)));
                                clicker.Movement.Unlock(false);
                            }, 1));
                        return;
                    }
                }
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}