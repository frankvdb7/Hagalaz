using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.GameObjects
{
    [GameObjectScriptMetaData([12266, 12268])]
    public class TrapDoorToSummoningObelisk : GameObjectScript
    {
        private readonly IGameObjectService _gameObjectService;

        public TrapDoorToSummoningObelisk(IGameObjectService gameObjectService)
        {
            _gameObjectService = gameObjectService;
        }

        /// <summary>
        ///     Happens on character click.
        /// </summary>
        /// <param name="clicker">Character which clicked on the object.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                if (Owner.Id == 12268)
                {
                    clicker.Interrupt(this);
                    clicker.Movement.Lock(true);
                    clicker.QueueAnimation(Animation.Create(827));
                    clicker.QueueTask(new RsTask(() =>
                        {
                            clicker.Movement.Teleport(Teleport.Create(Location.Create(2209, 5348, 0, 0)));
                            clicker.Movement.Unlock(false);
                        }, 2));
                }
                else
                {
                    _gameObjectService.UpdateGameObject(new GameObjectUpdate
                    {
                        Instance = Owner,
                        Id = 12268
                    });
                }

                return;
            }

            if (clickType == GameObjectClickType.Option2Click)
            {
                _gameObjectService.UpdateGameObject(new GameObjectUpdate
                {
                    Instance = Owner,
                    Id = 12266
                });
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }
    }
}