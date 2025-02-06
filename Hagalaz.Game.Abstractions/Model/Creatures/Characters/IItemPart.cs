using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface IItemPart
    {
        /// <summary>
        /// Contains the item Id.
        /// </summary>
        int ItemId { get; }
        /// <summary>
        /// Contains the male models.
        /// </summary>
        int[] MaleModels { get; }
        /// <summary>
        /// Contains the female models.
        /// </summary>
        int[] FemaleModels { get;}
        /// <summary>
        /// Contains the model colours.
        /// </summary>
        int[] ModelColors { get; }
        /// <summary>
        /// Contains the texture colours.
        /// </summary>
        int[] TextureColors { get; }
        /// <summary>
        /// Contains the flags.
        /// </summary>
        ItemUpdateFlags Flags { get; }
        /// <summary>
        /// Sets the model part.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="modelID">The model identifier.</param>
        /// <exception cref="System.Exception">Invalid model part!</exception>
        void SetModelPart(int part, int modelID);
        /// <summary>
        /// Sets the model part colour.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="colour">The colour.</param>
        /// <exception cref="System.Exception">Invalid model colour part!</exception>
        void SetModelPartColor(int part, int colour);
        /// <summary>
        /// Sets the model part texture.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="texture">The texture.</param>
        /// <exception cref="Exception">Invalid model texture part!</exception>
        void SetModelPartTexture(int part, int texture);
    }
}
