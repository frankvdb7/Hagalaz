using System;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public class ItemPart : IItemPart, IHydratable<HydratedItemAppearanceDto>, IDehydratable<HydratedItemAppearanceDto>
    {
        /// <summary>
        /// Contains the male models.
        /// </summary>
        public required int[] MaleModels { get; set; }

        /// <summary>
        /// Contains the female models.
        /// </summary>
        public required int[] FemaleModels { get; set; }

        /// <summary>
        /// Contains the model colours.
        /// </summary>
        public required int[] ModelColors { get; set; }

        /// <summary>
        /// Contains the texture colours.
        /// </summary>
        public required int[] TextureColors { get; set; }

        /// <summary>
        /// Contains the item Id.
        /// </summary>
        public int ItemId { get; }

        /// <summary>
        /// Contains the flags.
        /// </summary>
        public ItemUpdateFlags Flags { get; private set; }

        public ItemPart(int itemId)
        {
            ItemId = itemId;
        }

        /// <summary>
        /// Flags the specified flag.
        /// </summary>
        /// <param name="flag">The flag.</param>
        public void Flag(ItemUpdateFlags flag) => Flags |= flag;

        /// <summary>
        /// Uns the flag.
        /// </summary>
        /// <param name="flag">The flag.</param>
        public void UnFlag(ItemUpdateFlags flag) => Flags &= ~flag;

        /// <summary>
        /// Sets the model part.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="modelID">The model identifier.</param>
        /// <exception cref="System.Exception">Invalid model part!</exception>
        public void SetModelPart(int part, int modelID)
        {
            if (part < 0 || part >= MaleModels.Length || part >= FemaleModels.Length) throw new Exception("Invalid model part!");
            if (MaleModels[part] == modelID && FemaleModels[part] == modelID)
            {
                return;
            }

            MaleModels[part] = modelID;
            FemaleModels[part] = modelID;
            Flag(ItemUpdateFlags.Model);
        }

        /// <summary>
        /// Sets the model part colour.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="colour">The colour.</param>
        /// <exception cref="System.Exception">Invalid model colour part!</exception>
        public void SetModelPartColor(int part, int colour)
        {
            if (part < 0 || part >= ModelColors.Length) throw new Exception("Invalid model colour part!");
            if (ModelColors[part] == colour)
            {
                return;
            }

            ModelColors[part] = colour;
            Flag(ItemUpdateFlags.Color);
        }

        /// <summary>
        /// Sets the model part texture.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="texture">The texture.</param>
        /// <exception cref="Exception">Invalid model texture part!</exception>
        public void SetModelPartTexture(int part, int texture)
        {
            if (part < 0 || part >= ModelColors.Length) throw new Exception("Invalid model texture part!");
            if (TextureColors[part] == texture)
            {
                return;
            }

            TextureColors[part] = texture;
            Flag(ItemUpdateFlags.Texture);
        }

        public void Hydrate(HydratedItemAppearanceDto hydration)
        {
            MaleModels = hydration.MaleModels;
            FemaleModels = hydration.FemaleModels;
            ModelColors = hydration.ModelColors;
            TextureColors = hydration.TextureColors;
        }

        public HydratedItemAppearanceDto Dehydrate() =>
            new()
            {
                Id = ItemId,
                MaleModels = MaleModels,
                ModelColors = ModelColors,
                TextureColors = TextureColors,
                FemaleModels = FemaleModels
            };
    }
}