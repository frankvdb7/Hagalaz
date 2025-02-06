using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;
using Hagalaz.Game.Scripts.Widgets.Warning;

namespace Hagalaz.Game.Scripts.Areas.Wilderness.GameObjects
{
    [GameObjectScriptMetaData([37929, 38811])]
    public class CbPassage : GameObjectScript
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
                if (Owner.Id == 38811)
                {
                    if (clicker.Location.X == 2974)
                    {
                        clicker.Movement.Lock(true);
                        clicker.QueueAnimation(Animation.Create(827));
                        clicker.QueueTask(new RsTask(() =>
                            {
                                clicker.Movement.Teleport(Teleport.Create(Location.Create(2970, clicker.Location.Y, clicker.Location.Z, 0)));
                                clicker.Movement.Unlock(false);
                            }, 2));
                    }
                    else
                    {
                        var warningScript = clicker.ServiceProvider.GetRequiredService<WarningInterfaceScript>();
                        warningScript.OnAcceptClicked = () =>
                        {
                            clicker.Movement.Lock(true);
                            clicker.QueueAnimation(Animation.Create(827));
                            clicker.QueueTask(new RsTask(() =>
                                {
                                    clicker.Movement.Teleport(Teleport.Create(Location.Create(2974, clicker.Location.Y, clicker.Location.Z, 0)));
                                    clicker.Movement.Unlock(false);
                                }, 2));
                        };
                        clicker.Widgets.OpenWidget(650, 0, warningScript, false);
                    }
                }
                else
                {
                    clicker.Interrupt(this);
                    clicker.Movement.Lock(true);
                    clicker.QueueAnimation(Animation.Create(827));
                    clicker.QueueTask(new RsTask(() =>
                        {
                            clicker.Movement.Teleport(Teleport.Create(Location.Create(clicker.Location.X == 2917 ? 2921 : 2917, clicker.Location.Y, clicker.Location.Z, 0)));
                            clicker.Movement.Unlock(false);
                        }, 2));
                }

                return;
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