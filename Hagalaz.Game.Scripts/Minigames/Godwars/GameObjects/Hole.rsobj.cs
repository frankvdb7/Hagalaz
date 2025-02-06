﻿using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Items;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects
{
    /// <summary>
    ///     Contains the godwars hole script.
    /// </summary>
    public class Hole : GameObjectScript
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
                if (!clicker.HasState(StateType.HasGodWarsHoleRope))
                {
                    if (!clicker.Inventory.Contains(954))
                    {
                        clicker.SendChatMessage("You need a rope in order to climb down here.");
                        return;
                    }

                    clicker.QueueAnimation(Animation.Create(827));
                    clicker.AddState(new State(StateType.HasGodWarsHoleRope, int.MaxValue));
                    clicker.Inventory.Remove(new Item(954, 1));
                    ShowRope(clicker);
                }
                else
                {
                    clicker.Movement.Lock(true);
                    clicker.SendChatMessage("You climb down the rope...");
                    clicker.QueueAnimation(Animation.Create(827));
                    clicker.QueueTask(new RsTask(() =>
                        {
                            clicker.Movement.Teleport(Teleport.Create(Location.Create(2882, 5311, 0, 0)));
                            clicker.Movement.Unlock(false);
                        }, 2));
                }

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
            if (character.HasState(StateType.HasSaradominFirstRockRope))
            {
                ShowRope(character);
            }
        }

        /// <summary>
        ///     Shows the rope.
        /// </summary>
        private void ShowRope(ICharacter character) => character.Configurations.SendBitConfiguration(Owner.Definition.VarpBitFileId, 1);

        /// <summary>
        ///     Get's objectIDS which are suitable for this script.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableObjects() => [26341];

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}