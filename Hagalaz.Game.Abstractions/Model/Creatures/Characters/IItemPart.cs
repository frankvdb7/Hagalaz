using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for an object that holds the appearance information for a single item, such as its models, colors, and textures.
    /// </summary>
    public interface IItemPart
    {
        /// <summary>
        /// Gets the unique ID of the item this appearance belongs to.
        /// </summary>
        int ItemId { get; }
        /// <summary>
        /// Gets the array of model IDs used to render the item on a male character.
        /// </summary>
        int[] MaleModels { get; }
        /// <summary>
        /// Gets the array of model IDs used to render the item on a female character.
        /// </summary>
        int[] FemaleModels { get;}
        /// <summary>
        /// Gets the array of original model colors that can be recolored.
        /// </summary>
        int[] ModelColors { get; }
        /// <summary>
        /// Gets the array of original texture colors that can be retextured.
        /// </summary>
        int[] TextureColors { get; }
        /// <summary>
        /// Gets the bitmask of flags that indicate which parts of the item's appearance have been customized.
        /// </summary>
        ItemUpdateFlags Flags { get; }
        /// <summary>
        /// Sets the model ID for a specific part of the item's appearance.
        /// </summary>
        /// <param name="part">The index of the model part to set.</param>
        /// <param name="modelID">The new model ID.</param>
        void SetModelPart(int part, int modelID);
        /// <summary>
        /// Sets the color for a specific part of the item's model.
        /// </summary>
        /// <param name="part">The index of the model color to set.</param>
        /// <param name="colour">The new color value.</param>
        void SetModelPartColor(int part, int colour);
        /// <summary>
        /// Sets the texture for a specific part of the item's model.
        /// </summary>
        /// <param name="part">The index of the model texture to set.</param>
        /// <param name="texture">The new texture value.</param>
        void SetModelPartTexture(int part, int texture);
    }
}
