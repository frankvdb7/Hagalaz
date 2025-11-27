using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects.Saradomin
{
    /// <summary>
    /// </summary>
    [GameObjectScriptMetaData([26444])]
    public class FirstRockDown : GameObjectScript
    {
        private readonly IItemBuilder _itemBuilder;

        public FirstRockDown(IItemBuilder itemBuilder)
        {
            _itemBuilder = itemBuilder;
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
            switch (clickType)
            {
                case GameObjectClickType.Option1Click:
                {
                    clicker.Interrupt(this);
                    if (!clicker.HasState<HasSaradominFirstRockRopeState>())
                    {
                        if (!clicker.Inventory.Contains(954))
                        {
                            clicker.SendChatMessage("You need a rope in order to climb down here.");
                            return;
                        }

                        clicker.QueueAnimation(Animation.Create(827));
                        clicker.AddState(new HasSaradominFirstRockRopeState { TicksLeft = int.MaxValue });
                        clicker.Inventory.Remove(_itemBuilder.Create().WithId(954).Build());
                        ShowRope(clicker);
                        return;
                    }

                    break;
                }
                case GameObjectClickType.Option2Click when clicker.HasState<HasSaradominFirstRockRopeState>():
                    clicker.Movement.Lock(true);
                    clicker.SendChatMessage("You climb down the rope...");
                    clicker.QueueAnimation(Animation.Create(827));
                    clicker.QueueTask(new RsTask(() =>
                        {
                            clicker.Movement.Teleport(Teleport.Create(Location.Create(2915, 5300, 1, 0)));
                            clicker.Movement.Unlock(false);
                        }, 2));
                    return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Happens when this object is rendered for a specific character.
        ///     By default this method does nothing.
        /// </summary>
        /// <param name="character">The character.</param>
        public override void OnRenderedFor(ICharacter character)
        {
            if (character.HasState<HasSaradominFirstRockRopeState>())
            {
                ShowRope(character);
            }
        }

        /// <summary>
        ///     Shows the rope.
        /// </summary>
        private void ShowRope(ICharacter character) => character.Configurations.SendBitConfiguration(Owner.Definition.VarpBitFileId, 1);
    }
}