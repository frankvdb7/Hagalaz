using System.Collections.Generic;
using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Represents the type definition for an in-game item.
    /// </summary>
    public interface IItemType
    {
        /// <summary>
        /// Gets the unique identifier for this item type.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the name of the item.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a value indicating whether this item is in its noted form.
        /// </summary>
        bool Noted { get; }

        /// <summary>
        /// Gets a value indicating whether this item can be stacked in the inventory.
        /// </summary>
        bool Stackable { get; }

        /// <summary>
        /// Gets the ID of the noted version of this item.
        /// </summary>
        int NoteId { get; }

        /// <summary>
        /// Gets the render animation ID for this item.
        /// </summary>
        /// <returns>The render animation ID, or -1 if not applicable.</returns>
        int RenderAnimationId { get; }

        /// <summary>
        /// Gets a value indicating whether the item has a "destroy" option.
        /// </summary>
        bool HasDestroyOption { get; }

        /// <summary>
        /// Gets the template ID for the noted version of this item.
        /// </summary>
        int NoteTemplateId { get; }

        /// <summary>
        /// Gets the template ID for the lent version of this item.
        /// </summary>
        int LendTemplateId { get; }

        /// <summary>
        /// Gets the template ID for the bought version of this item.
        /// </summary>
        int BoughtTemplateId { get; }

        /// <summary>
        /// Gets the ID of the bought version of this item.
        /// </summary>
        int BoughtItemId { get; }

        /// <summary>
        /// Gets the ID of the lent version of this item.
        /// </summary>
        int LendId { get; }

        /// <summary>
        /// Gets the base monetary value of the item.
        /// </summary>
        int Value { get; }

        /// <summary>
        /// Gets the degradation type of the item when dropped.
        /// </summary>
        DegradeType DegradeType { get; }

        /// <summary>
        /// Gets a value indicating whether this item is for members only.
        /// </summary>
        bool MembersOnly { get; }

        /// <summary>
        /// Gets the primary model ID for male characters wearing this item.
        /// </summary>
        int MaleWornModelId1 { get; }

        /// <summary>
        /// Gets the secondary model ID for male characters wearing this item.
        /// </summary>
        int MaleWornModelId2 { get; }

        /// <summary>
        /// Gets the primary model ID for female characters wearing this item.
        /// </summary>
        int FemaleWornModelId1 { get; }

        /// <summary>
        /// Gets the secondary model ID for female characters wearing this item.
        /// </summary>
        int FemaleWornModelId2 { get; }

        /// <summary>
        /// Gets the primary head model ID for male characters wearing this item.
        /// </summary>
        int MaleHeadModel { get; }

        /// <summary>
        /// Gets the primary head model ID for female characters wearing this item.
        /// </summary>
        int FemaleHeadModel { get; }

        /// <summary>
        /// Gets the secondary head model ID for male characters wearing this item.
        /// </summary>
        int MaleHeadModel2 { get; }

        /// <summary>
        /// Gets the secondary head model ID for female characters wearing this item.
        /// </summary>
        int FemaleHeadModel2 { get; }

        /// <summary>
        /// Gets the X-axis offset for male characters wearing this item.
        /// </summary>
        int MaleWearXOffset { get; }

        /// <summary>
        /// Gets the Y-axis offset for male characters wearing this item.
        /// </summary>
        int MaleWearYOffset { get; }

        /// <summary>
        /// Gets the Z-axis offset for male characters wearing this item.
        /// </summary>
        int MaleWearZOffset { get; }

        /// <summary>
        /// Gets the X-axis offset for female characters wearing this item.
        /// </summary>
        int FemaleWearXOffset { get; }

        /// <summary>
        /// Gets the Y-axis offset for female characters wearing this item.
        /// </summary>
        int FemaleWearYOffset { get; }

        /// <summary>
        /// Gets the Z-axis offset for female characters wearing this item.
        /// </summary>
        int FemaleWearZOffset { get; }

        /// <summary>
        /// Gets the model ID used for rendering the item in interfaces.
        /// </summary>
        int InterfaceModelId { get; }

        /// <summary>
        /// Gets the original colors of the item's model.
        /// </summary>
        int[]? OriginalModelColors { get; }

        /// <summary>
        /// Gets the modified colors of the item's model.
        /// </summary>
        int[]? ModifiedModelColors { get; }

        /// <summary>
        /// Gets the original texture colors of the item's model.
        /// </summary>
        int[]? OriginalTextureColors { get; }

        /// <summary>
        /// Gets the modified texture colors of the item's model.
        /// </summary>
        int[]? ModifiedTextureColors { get; }

        /// <summary>
        /// Gets the first model offset value.
        /// </summary>
        int ModelOffset1 { get; }

        /// <summary>
        /// Gets the second model offset value.
        /// </summary>
        int ModelOffset2 { get; }

        /// <summary>
        /// Gets the tertiary model ID for male characters wearing this item.
        /// </summary>
        int MaleWornModelId3 { get; }

        /// <summary>
        /// Gets the tertiary model ID for female characters wearing this item.
        /// </summary>
        int FemaleWornModelId3 { get; }

        /// <summary>
        /// Gets the available options for the item when on the ground.
        /// </summary>
        string?[] GroundOptions { get; }

        /// <summary>
        /// Gets additional key-value data associated with the item.
        /// </summary>
        IReadOnlyDictionary<int, object>? ExtraData { get; }

        /// <summary>
        /// Gets the zoom level of the item's model.
        /// </summary>
        int ModelZoom { get; }

        /// <summary>
        /// Gets a 2D angle-related value.
        /// </summary>
        int Zan2D { get; }

        /// <summary>
        /// Gets an unknown byte array.
        /// </summary>
        sbyte[]? UnknownArray1 { get; }

        /// <summary>
        /// Gets the second model rotation value.
        /// </summary>
        int ModelRotation2 { get; }

        /// <summary>
        /// Gets the available options for the item when in the inventory.
        /// </summary>
        string?[] InventoryOptions { get; }

        /// <summary>
        /// Gets the team ID associated with this item.
        /// </summary>
        int TeamId { get; }

        /// <summary>
        /// Gets the equipment slot ID for this item.
        /// </summary>
        sbyte EquipSlot { get; }

        /// <summary>
        /// Gets the equipment type ID for this item.
        /// </summary>
        sbyte EquipType { get; }

        /// <summary>
        /// Gets the first model rotation value.
        /// </summary>
        int ModelRotation1 { get; }

        /// <summary>
        /// Gets the stackable type of the item.
        /// </summary>
        int StackableType { get; }

        /// <summary>
        /// Determines whether this item has a special attack bar.
        /// </summary>
        /// <returns><c>true</c> if the item has a special attack bar; otherwise, <c>false</c>.</returns>
        bool HasSpecialBar();

        /// <summary>
        /// Gets the quest ID associated with this item.
        /// </summary>
        /// <returns>The quest ID, or -1 if not applicable.</returns>
        int GetQuestId();

        /// <summary>
        /// Gets the attack bonus types for this weapon.
        /// </summary>
        /// <returns>An array of attack bonus type IDs.</returns>
        int[] GetAttackBonusTypes();

        /// <summary>
        /// Gets the attack style types for this weapon.
        /// </summary>
        /// <returns>An array of attack style type IDs.</returns>
        int[] GetAttackStylesTypes();

        /// <summary>
        /// Gets the skill level requirements for equipping this item.
        /// </summary>
        /// <returns>A dictionary where the key is the skill ID and the value is the required level.</returns>
        IReadOnlyDictionary<int, int> GetEquipmentRequirements();

        /// <summary>
        /// Gets the skill level requirements for creating this item.
        /// </summary>
        /// <returns>A dictionary where the key is the skill ID and the value is the required level, or null if not applicable.</returns>
        IReadOnlyDictionary<int, int>? GetCreateItemRequirements();

        /// <summary>
        /// Gets the attack speed of the weapon.
        /// </summary>
        /// <returns>The attack speed value.</returns>
        int GetAttackSpeed();

        /// <summary>
        /// Gets the stab attack bonus.
        /// </summary>
        /// <returns>The stab attack bonus.</returns>
        int GetStabAttack();

        /// <summary>
        /// Gets the slash attack bonus.
        /// </summary>
        /// <returns>The slash attack bonus.</returns>
        int GetSlashAttack();

        /// <summary>
        /// Gets the crush attack bonus.
        /// </summary>
        /// <returns>The crush attack bonus.</returns>
        int GetCrushAttack();

        /// <summary>
        /// Gets the magic attack bonus.
        /// </summary>
        /// <returns>The magic attack bonus.</returns>
        int GetMagicAttack();

        /// <summary>
        /// Gets the ranged attack bonus.
        /// </summary>
        /// <returns>The ranged attack bonus.</returns>
        int GetRangeAttack();

        /// <summary>
        /// Gets the stab defence bonus.
        /// </summary>
        /// <returns>The stab defence bonus.</returns>
        int GetStabDefence();

        /// <summary>
        /// Gets the slash defence bonus.
        /// </summary>
        /// <returns>The slash defence bonus.</returns>
        int GetSlashDefence();

        /// <summary>
        /// Gets the crush defence bonus.
        /// </summary>
        /// <returns>The crush defence bonus.</returns>
        int GetCrushDefence();

        /// <summary>
        /// Gets the magic defence bonus.
        /// </summary>
        /// <returns>The magic defence bonus.</returns>
        int GetMagicDefence();

        /// <summary>
        /// Gets the ranged defence bonus.
        /// </summary>
        /// <returns>The ranged defence bonus.</returns>
        int GetRangeDefence();

        /// <summary>
        /// Gets the summoning defence bonus.
        /// </summary>
        /// <returns>The summoning defence bonus.</returns>
        int GetSummoningDefence();

        /// <summary>
        /// Gets the melee absorption bonus.
        /// </summary>
        /// <returns>The melee absorption bonus.</returns>
        int GetAbsorbMeleeBonus();

        /// <summary>
        /// Gets the magic absorption bonus.
        /// </summary>
        /// <returns>The magic absorption bonus.</returns>
        int GetAbsorbMageBonus();

        /// <summary>
        /// Gets the ranged absorption bonus.
        /// </summary>
        /// <returns>The ranged absorption bonus.</returns>
        int GetAbsorbRangeBonus();

        /// <summary>
        /// Gets the strength bonus.
        /// </summary>
        /// <returns>The strength bonus.</returns>
        int GetStrengthBonus();

        /// <summary>
        /// Gets the ranged strength bonus.
        /// </summary>
        /// <returns>The ranged strength bonus.</returns>
        int GetRangedStrengthBonus();

        /// <summary>
        /// Gets the magic damage bonus.
        /// </summary>
        /// <returns>The magic damage bonus.</returns>
        int GetMagicDamage();

        /// <summary>
        /// Gets the prayer bonus.
        /// </summary>
        /// <returns>The prayer bonus.</returns>
        int GetPrayerBonus();

        /// <summary>
        /// Configures this item type as a noted item based on a template.
        /// </summary>
        /// <param name="item">The original item type.</param>
        /// <param name="template">The noted item template.</param>
        void MakeNote(IItemType item, IItemType template);

        /// <summary>
        /// Configures this item type as a lent item based on a template.
        /// </summary>
        /// <param name="item">The original item type.</param>
        /// <param name="template">The lent item template.</param>
        void MakeLend(IItemType item, IItemType template);

        /// <summary>
        /// Configures this item type as a bought item based on a template.
        /// </summary>
        /// <param name="item">The original item type.</param>
        /// <param name="template">The bought item template.</param>
        void MakeBought(IItemType item, IItemType template);
    }
}