using System.Collections.Generic;
using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Represents the definition of an in-game item, including its properties, models, and behaviors.
    /// This interface provides a read-only view of an item's data as stored in the cache.
    /// </summary>
    public interface IItemType : IType
    {
        /// <summary>
        /// Gets the unique identifier for this item type.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the name of the item as it appears in-game.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a value indicating whether this item is in its noted form, which is a stackable representation of an unstackable item.
        /// </summary>
        bool Noted { get; }

        /// <summary>
        /// Gets a value indicating whether this item can be stacked in a single inventory slot.
        /// </summary>
        bool Stackable { get; }

        /// <summary>
        /// Gets the item ID of the noted version of this item. If the item is already noted, this will be its own ID.
        /// </summary>
        int NoteId { get; }

        /// <summary>
        /// Gets the render animation ID used when this item is equipped, which determines its idle animation.
        /// </summary>
        /// <returns>The render animation ID, or -1 if the item does not have a specific render animation.</returns>
        int RenderAnimationId { get; }

        /// <summary>
        /// Gets a value indicating whether the item has a "destroy" option in the inventory.
        /// </summary>
        bool HasDestroyOption { get; }

        /// <summary>
        /// Gets the template ID used to create the noted version of this item.
        /// </summary>
        int NoteTemplateId { get; }

        /// <summary>
        /// Gets the template ID used to create a lent version of this item.
        /// </summary>
        int LendTemplateId { get; }

        /// <summary>
        /// Gets the template ID used to create a "bought" version of this item, typically from a shop.
        /// </summary>
        int BoughtTemplateId { get; }

        /// <summary>
        /// Gets the item ID of the "bought" version of this item.
        /// </summary>
        int BoughtItemId { get; }

        /// <summary>
        /// Gets the item ID of the lent version of this item.
        /// </summary>
        int LendId { get; }

        /// <summary>
        /// Gets the base monetary value of the item, often used for alchemy or shop prices.
        /// </summary>
        int Value { get; }

        /// <summary>
        /// Gets the behavior of the item when dropped, such as being destroyed or becoming visible to other players.
        /// </summary>
        DegradeType DegradeType { get; }

        /// <summary>
        /// Gets a value indicating whether this item is exclusive to members.
        /// </summary>
        bool MembersOnly { get; }

        /// <summary>
        /// Gets the primary model ID for male characters wearing this item.
        /// </summary>
        int MaleWornModelId1 { get; }

        /// <summary>
        /// Gets the secondary model ID for male characters, often used for arms or other additional parts.
        /// </summary>
        int MaleWornModelId2 { get; }

        /// <summary>
        /// Gets the primary model ID for female characters wearing this item.
        /// </summary>
        int FemaleWornModelId1 { get; }

        /// <summary>
        /// Gets the secondary model ID for female characters, often used for arms or other additional parts.
        /// </summary>
        int FemaleWornModelId2 { get; }

        /// <summary>
        /// Gets the head model ID for male characters when this item is equipped, often for helmets or masks.
        /// </summary>
        int MaleHeadModel { get; }

        /// <summary>
        /// Gets the head model ID for female characters when this item is equipped.
        /// </summary>
        int FemaleHeadModel { get; }

        /// <summary>
        /// Gets the secondary head model ID for male characters.
        /// </summary>
        int MaleHeadModel2 { get; }

        /// <summary>
        /// Gets the secondary head model ID for female characters.
        /// </summary>
        int FemaleHeadModel2 { get; }

        /// <summary>
        /// Gets the X-axis offset for the male worn model.
        /// </summary>
        int MaleWearXOffset { get; }

        /// <summary>
        /// Gets the Y-axis offset for the male worn model.
        /// </summary>
        int MaleWearYOffset { get; }

        /// <summary>
        /// Gets the Z-axis offset for the male worn model.
        /// </summary>
        int MaleWearZOffset { get; }

        /// <summary>
        /// Gets the X-axis offset for the female worn model.
        /// </summary>
        int FemaleWearXOffset { get; }

        /// <summary>
        /// Gets the Y-axis offset for the female worn model.
        /// </summary>
        int FemaleWearYOffset { get; }

        /// <summary>
        /// Gets the Z-axis offset for the female worn model.
        /// </summary>
        int FemaleWearZOffset { get; }

        /// <summary>
        /// Gets the model ID used for rendering the item in interfaces like the inventory or bank.
        /// </summary>
        int InterfaceModelId { get; }

        /// <summary>
        /// Gets the original colors of the item's model that are replaced by <see cref="ModifiedModelColors"/>.
        /// </summary>
        int[]? OriginalModelColors { get; }

        /// <summary>
        /// Gets the new colors that replace the <see cref="OriginalModelColors"/> in the item's model.
        /// </summary>
        int[]? ModifiedModelColors { get; }

        /// <summary>
        /// Gets the original texture colors of the item's model that are replaced by <see cref="ModifiedTextureColors"/>.
        /// </summary>
        int[]? OriginalTextureColors { get; }

        /// <summary>
        /// Gets the new texture colors that replace the <see cref="OriginalTextureColors"/> in the item's model.
        /// </summary>
        int[]? ModifiedTextureColors { get; }

        /// <summary>
        /// Gets the first model offset value, used for fine-tuning the model's position.
        /// </summary>
        int ModelOffset1 { get; }

        /// <summary>
        /// Gets the second model offset value.
        /// </summary>
        int ModelOffset2 { get; }

        /// <summary>
        /// Gets the tertiary model ID for male characters, used for complex items.
        /// </summary>
        int MaleWornModelId3 { get; }

        /// <summary>
        /// Gets the tertiary model ID for female characters.
        /// </summary>
        int FemaleWornModelId3 { get; }

        /// <summary>
        /// Gets the array of options available for the item when it is on the ground.
        /// </summary>
        string?[] GroundOptions { get; }

        /// <summary>
        /// Gets a dictionary of additional key-value data associated with the item, used for custom properties.
        /// </summary>
        IReadOnlyDictionary<int, object>? ExtraData { get; }

        /// <summary>
        /// Gets the zoom level of the item's model in the inventory interface.
        /// </summary>
        int ModelZoom { get; }

        /// <summary>
        /// Gets a 2D rotation value for the item's model.
        /// </summary>
        int Zan2D { get; }

        /// <summary>
        /// Gets an array of signed bytes containing unknown or legacy data.
        /// </summary>
        sbyte[]? UnknownArray1 { get; }

        /// <summary>
        /// Gets the second model rotation value.
        /// </summary>
        int ModelRotation2 { get; }

        /// <summary>
        /// Gets the array of options available for the item when it is in the inventory.
        /// </summary>
        string?[] InventoryOptions { get; }

        /// <summary>
        /// Gets the team ID this item is associated with, used in certain minigames.
        /// </summary>
        int TeamId { get; }

        /// <summary>
        /// Gets the equipment slot where this item is worn.
        /// </summary>
        sbyte EquipSlot { get; }

        /// <summary>
        /// Gets the type of equipment, which can affect which other items can be worn.
        /// </summary>
        sbyte EquipType { get; }

        /// <summary>
        /// Gets the first model rotation value.
        /// </summary>
        int ModelRotation1 { get; }

        /// <summary>
        /// Gets the stackable type of the item, where 1 indicates it is always stackable.
        /// </summary>
        int StackableType { get; }


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