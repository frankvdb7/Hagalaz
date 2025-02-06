using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.GameObjects
{
    /// <summary>
    ///     Contains trap door script.
    /// </summary>
    [GameObjectScriptMetaData([26933, 26934])]
    public class TrapDoorToTrainingBegin : GameObjectScript
    {
        private readonly IGameObjectService _gameObjectService;

        public TrapDoorToTrainingBegin(IGameObjectService gameObjectService)
        {
            _gameObjectService = gameObjectService;
        }

        /// <summary>
        ///     Happens on character click.
        /// </summary>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                if (Owner.Id == 26934)
                {
                    clicker.Interrupt(this);
                    clicker.Movement.Lock(true);
                    clicker.QueueAnimation(Animation.Create(827));
                    clicker.QueueTask(new RsTask(() =>
                        {
                            clicker.Movement.Teleport(Teleport.Create(Location.Create(2885, 9795, 0, 0)));
                            clicker.Movement.Unlock(false);
                        },
                        2));
                }
                else
                {
                    _gameObjectService.UpdateGameObject(new GameObjectUpdate
                    {
                        Instance = Owner,
                        Id = 26934
                    });
                }
                return;
            }

            if (clickType == GameObjectClickType.Option2Click)
            {
                _gameObjectService.UpdateGameObject(new GameObjectUpdate
                {
                    Instance = Owner,
                    Id = 26933
                });
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }
    }
}