using System;
using System.Globalization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Equipment.Auras;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Auras
{
    [ItemScriptMetaData([])]
    public abstract class AuraItemScript : ItemScript
    {
        /// <summary>
        ///     Items the clicked in inventory.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="aura">The aura.</param>
        /// <param name="character">The character.</param>
        public override void ItemClickedInInventory(ComponentClickType clickType, IItem aura, ICharacter character)
        {
            if (clickType == ComponentClickType.Option3Click)
            {
                SendTimeRemaining(character, aura);
                return;
            }

            base.ItemClickedInInventory(clickType, aura, character);
        }

        /// <summary>
        ///     Items the clicked in equipment.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="aura">The aura.</param>
        /// <param name="character">The character.</param>
        public override void ItemClickedInEquipment(ComponentClickType clickType, IItem aura, ICharacter character)
        {
            if (clickType == ComponentClickType.Option2Click)
            {
                if (IsActivated(aura))
                {
                    character.SendChatMessage("You cannot deactivate an aura."); // TODO find out real message
                    return;
                }

                ToggleAura(character, aura);
                return;
            }

            if (clickType == ComponentClickType.Option3Click)
            {
                SendTimeRemaining(character, aura);
                return;
            }

            if (clickType == ComponentClickType.Option4Click)
            {
                aura.EquipmentScript.UnEquipItem(aura, character);
                return;
            }

            base.ItemClickedInEquipment(clickType, aura, character);
        }

        /// <summary>
        ///     Sends the time remaining.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="aura">The aura.</param>
        public void SendTimeRemaining(ICharacter character, IItem aura)
        {
            if (IsActivated(aura))
            {
                character.SendChatMessage("Currently active. <col=00FF00>" + GetRemainingTimeAsString(aura) + " remaining.");
                return;
            }

            if (HasCoolDown(aura))
            {
                character.SendChatMessage("Currently recharging. <col=ff0000>" + GetRemainingTimeAsString(aura) + " until fully recharged.");
                return;
            }

            character.SendChatMessage("The aura has finished recharging. It is ready to use.");
        }


        /// <summary>
        ///     Determines whether [has cool down] [the specified aura].
        /// </summary>
        /// <param name="aura">The aura.</param>
        /// <returns>
        ///     <c>true</c> if [has cool down] [the specified aura]; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasCoolDown(IItem aura)
        {
            var endTicks = aura.ExtraData[1];
            if (endTicks <= 0)
            {
                return false;
            }

            if (DateTime.UtcNow.Ticks > endTicks)
            {
                return false;
            }

            return aura.ExtraData[0] == 0;
        }

        /// <summary>
        ///     Determines whether [is aura activated] [the specified aura].
        /// </summary>
        /// <param name="aura">The aura.</param>
        /// <returns>
        ///     <c>true</c> if [is aura activated] [the specified aura]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsActivated(IItem aura)
        {
            if (aura.ExtraData[0] == 0)
            {
                return false;
            }

            var endTicks = aura.ExtraData[1];
            if (endTicks <= 0)
            {
                return false;
            }

            return aura.ExtraData[0] == 1 && DateTime.UtcNow.Ticks < endTicks;
        }

        /// <summary>
        ///     Gets the remaining time as string.
        /// </summary>
        /// <param name="aura">The aura.</param>
        /// <returns></returns>
        public string GetRemainingTimeAsString(IItem aura) => TimeSpanToString(TimeSpan.FromTicks(aura.ExtraData[1] - DateTime.UtcNow.Ticks));

        /// <summary>
        ///     Toggles the aura.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="aura">The aura.</param>
        public virtual void ToggleAura(ICharacter character, IItem aura)
        {
            if (IsActivated(aura))
            {
                Deactivate(character, aura);
            }
            else
            {
                Activate(character, aura);
            }
        }

        /// <summary>
        ///     Activates the aura.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="aura">The aura.</param>
        public virtual void Activate(ICharacter character, IItem aura)
        {
            if (HasCoolDown(aura))
            {
                SendTimeRemaining(character, aura);
                return;
            }

            var tier = GetTier(character, aura);
            character.QueueAnimation(Animation.Create(2231));
            character.QueueGraphic(Graphic.Create(tier switch
            {
                3 => 1763,
                2 => 1764,
                _ => 370
            }));

            if (aura.EquipmentScript is AuraEquipmentScript auraEquipmentScript)
            {
                auraEquipmentScript.DrawModel(character, aura);
            }

            var endTime = IsActivated(aura) ? new DateTime(aura.ExtraData[1], DateTimeKind.Utc) : DateTime.UtcNow.Add(GetActivationTime(character, aura));

            // set the activation to true
            aura.ExtraData[0] = 1;
            // set the active time
            aura.ExtraData[1] = endTime.Ticks;

            var disableTick = (int)((endTime - DateTime.UtcNow).TotalMilliseconds / 600);
            RsTickTask tickTask = null;
            tickTask = new RsTickTask(() =>
            {
                if (tickTask.TickCount >= disableTick)
                {
                    Deactivate(character, aura);
                    tickTask.Cancel();
                    return;
                }

                if (aura.ExtraData[0] == 0)
                {
                    tickTask.Cancel();
                }
            });
            character.QueueTask(tickTask);
        }

        /// <summary>
        ///     Deactivates the aura.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="aura">The aura.</param>
        public virtual void Deactivate(ICharacter character, IItem aura)
        {
            // set the activation to false
            aura.ExtraData[0] = 0;
            // set the cooldown time
            aura.ExtraData[1] = DateTime.UtcNow.Add(GetCoolDownTime(character, aura)).Ticks;

            if (aura.EquipmentScript is AuraEquipmentScript script)
            {
                AuraEquipmentScript.ClearModel(character, aura);
            }

            character.SendChatMessage("Your aura has been deactivated."); // TODO find real message
        }

        /// <summary>
        ///     Gets the tier.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="aura">The aura.</param>
        /// <returns></returns>
        public abstract int GetTier(ICharacter character, IItem aura);

        /// <summary>
        ///     Gets the actvation time.
        ///     Default is 1 hour.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="aura">The aura.</param>
        /// <returns></returns>
        public virtual TimeSpan GetActivationTime(ICharacter character, IItem aura) => TimeSpan.FromHours(1);

        /// <summary>
        ///     Gets the cool down time.
        ///     Default is 3 hours.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="aura">The aura.</param>
        /// <returns></returns>
        public virtual TimeSpan GetCoolDownTime(ICharacter character, IItem aura) => TimeSpan.FromHours(3);

        /// <summary>
        ///     Times the span to string.
        /// </summary>
        /// <param name="timeSpan">The time span.</param>
        /// <returns></returns>
        protected static string TimeSpanToString(TimeSpan timeSpan) => timeSpan.ToString(@"h\h\ m\m\ s\s", CultureInfo.InvariantCulture);
    }
}