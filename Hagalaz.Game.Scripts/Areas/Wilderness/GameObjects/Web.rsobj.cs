using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.Wilderness.GameObjects
{
    [GameObjectScriptMetaData([733])]
    public class Web : GameObjectScript
    {
        private readonly IGameObjectService _gameObjectService;

        public Web(IGameObjectService gameObjectService)
        {
            _gameObjectService = gameObjectService;
        }

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
                var weapon = clicker.Equipment[EquipmentSlot.Weapon];
                if (weapon == null)
                {
                    return;
                }

                var slashOptionId = -1;

                for (var i = 0; i < 4; i++)
                {
                    if (weapon.EquipmentDefinition.GetAttackBonus(i) == AttackBonus.Slash)
                    {
                        slashOptionId = i;
                        break;
                    }
                }

                if (slashOptionId == -1)
                {
                    clicker.SendChatMessage("You need to have a weapon equiped that is able to slash this web!");
                    return;
                }

                clicker.QueueAnimation(Animation.Create(weapon.EquipmentDefinition.GetAttackAnimationId(slashOptionId)));

                _gameObjectService.UpdateGameObject(new GameObjectUpdate
                {
                    Instance = Owner,
                    Id = Owner.Id + 1
                });
                Owner.Region.UnFlagCollision(Owner);
                clicker.QueueTask(new RsTask(() =>
                    {
                        _gameObjectService.UpdateGameObject(new GameObjectUpdate
                        {
                            Instance = Owner,
                            Id = Owner.Id - 1
                        });
                        Owner.Region.FlagCollision(Owner);
                    },
                    100));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize() { }
    }
}