using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Dialogues.Generic;
using Hagalaz.Game.Scripts.Items.Auras;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Auras
{
    /// <summary>
    /// </summary>
    public abstract class AuraEquipmentScript : EquipmentScript
    {
        /// <summary>
        ///     Uns the equip item.
        /// </summary>
        /// <param name="aura">The aura.</param>
        /// <param name="character">The character.</param>
        /// <param name="toInventorySlot">To inventory slot.</param>
        /// <returns></returns>
        public override bool UnEquipItem(IItem aura, ICharacter character, int toInventorySlot = -1)
        {
            if (aura.ItemScript is not AuraItemScript script)
            {
                return base.UnEquipItem(aura, character, toInventorySlot);
            }

            if (!AuraItemScript.IsActivated(aura))
            {
                return base.UnEquipItem(aura, character, toInventorySlot);
            }

            var yesNoDialogueScript = character.ServiceProvider.GetRequiredService<YesNoDialogueScript>();
            yesNoDialogueScript.Question = "The aura is active, are you sure you want to remove it?";
            yesNoDialogueScript.Callback = yes =>
            {
                if (yes)
                {
                    character.Equipment.UnEquipItem(aura, toInventorySlot);
                }

                character.Widgets.CloseChatboxOverlay();
            };
            character.Widgets.OpenDialogue(yesNoDialogueScript, true);
            return false;

        }

        /// <summary>
        ///     Called when [equiped].
        /// </summary>
        /// <param name="aura">The aura.</param>
        /// <param name="character">The character.</param>
        public override void OnEquiped(IItem aura, ICharacter character)
        {
            if (aura.ItemScript is not AuraItemScript script)
            {
                return;
            }

            if (AuraItemScript.IsActivated(aura))
            {
                script.Activate(character, aura);
            }
        }

        /// <summary>
        ///     Called when [unequiped].
        /// </summary>
        /// <param name="aura">The aura.</param>
        /// <param name="character">The character.</param>
        public override void OnUnequiped(IItem aura, ICharacter character)
        {
            if (aura.ItemScript is not AuraItemScript script)
            {
                return;
            }

            if (AuraItemScript.IsActivated(aura))
            {
                script.Deactivate(character, aura);
            }
        }

        /// <summary>
        ///     Draws the model.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="aura">The aura.</param>
        public void DrawModel(ICharacter character, IItem aura)
        {
            var factory = character.ServiceProvider.GetRequiredService<IItemPartFactory>();
            var itemPart = factory.Create(aura.Id);
            if (itemPart.MaleModels[0] != -1 && itemPart.FemaleModels[0] != -1)
            {
                itemPart.SetModelPart(0, GetMainModelID(character, aura));
            }

            if (itemPart.MaleModels[1] != -1 && itemPart.FemaleModels[1] != -1)
            {
                itemPart.SetModelPart(1, GetSecondaryModelID(character, aura));
            }

            character.Appearance.DrawItem(BodyPart.AuraPart, itemPart);
        }

        /// <summary>
        ///     Clears the model.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="aura">The aura.</param>
        public static void ClearModel(ICharacter character, IItem aura) => character.Appearance.ClearDrawnBodyPart(BodyPart.AuraPart);

        /// <summary>
        ///     Gets the model id1.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="aura">The aura.</param>
        /// <returns></returns>
        public static int GetMainModelID(ICharacter character, IItem aura)
        {
            var weapon = character.Equipment[EquipmentSlot.Weapon];
            if (weapon != null)
            {
                var name = weapon.Name.ToLower();
                if (name.Contains("dagger"))
                {
                    return 8724;
                }

                if (name.Contains("whip"))
                {
                    return 8725;
                }

                if (name.Contains("2h sword") || name.Contains("godsword"))
                {
                    return 8773;
                }

                if (name.Contains("sword") || name.Contains("scimitar"))
                {
                    return 8722;
                }
            }

            return 8719;
        }

        /// <summary>
        ///     Gets the model identifier.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="aura">The aura.</param>
        /// <returns></returns>
        public virtual int GetSecondaryModelID(ICharacter character, IItem aura) => -1;
    }
}