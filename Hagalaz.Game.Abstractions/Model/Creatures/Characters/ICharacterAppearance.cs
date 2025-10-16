namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for managing a player character's visual appearance, including clothing, equipment, and transformations.
    /// </summary>
    public interface ICharacterAppearance : ICreatureAppearance
    {
        /// <summary>
        /// Gets or sets a value indicating the character's gender. <c>true</c> for female, <c>false</c> for male.
        /// </summary>
        bool Female { get; set; }
        /// <summary>
        /// Gets the ID of the NPC this character is currently transformed into, or -1 if not transformed.
        /// </summary>
        int NpcId { get; }
        /// <summary>
        /// Gets or sets the character's render ID, which is used to uniquely identify their appearance combination.
        /// </summary>
        int RenderId { get; set; }
        /// <summary>
        /// Gets or sets the prayer icon displayed above the character's head.
        /// </summary>
        PrayerIcon PrayerIcon { get; set; }
        /// <summary>
        /// Gets or sets the skull icon displayed above the character's head.
        /// </summary>
        SkullIcon SkullIcon { get; set; }
        /// <summary>
        /// Gets or sets the title displayed next to the character's name.
        /// </summary>
        DisplayTitle DisplayTitle { get; set; }
        /// <summary>
        /// Gets the script that controls the character's behavior while transformed into an NPC (Player-NPC).
        /// </summary>
        ICharacterNpcScript PnpcScript { get; }
        /// <summary>
        /// Gets the appearance information for an item drawn on a specific body part.
        /// </summary>
        /// <param name="part">The body part to check.</param>
        /// <returns>The <see cref="IItemPart"/> for the item, or <c>null</c> if no item is drawn on that part.</returns>
        IItemPart? GetDrawnItemPart(BodyPart part);
        /// <summary>
        /// Clears any item drawn on a specific body part, reverting it to the character's base appearance.
        /// </summary>
        /// <param name="part">The body part to clear.</param>
        void ClearDrawnBodyPart(BodyPart part);
        /// <summary>
        /// Draws an item's appearance on a specific body part.
        /// </summary>
        /// <param name="part">The body part on which to draw the item.</param>
        /// <param name="itemPart">The appearance information for the item.</param>
        void DrawItem(BodyPart part, IItemPart itemPart);
        /// <summary>
        /// Transforms the character's appearance into that of an NPC.
        /// </summary>
        /// <param name="npcId">The ID of the NPC to transform into.</param>
        /// <param name="pnpcScript">An optional script to control the character's behavior while transformed.</param>
        void TurnToNpc(int npcId, ICharacterNpcScript? pnpcScript = null);
        /// <summary>
        /// Reverts the character's appearance from an NPC transformation back to their normal player look.
        /// </summary>
        void TurnToPlayer();
        /// <summary>
        /// Gets the current color value for a specific customizable part of the character's appearance.
        /// </summary>
        /// <param name="type">The type of color to retrieve (e.g., hair, skin).</param>
        /// <returns>The color value.</returns>
        int GetColor(ColorType type);
        /// <summary>
        /// Retrieves the appearance information for a given item ID, adding it to the cache if not already present.
        /// </summary>
        /// <param name="itemId">The ID of the item.</param>
        /// <returns>The <see cref="IItemPart"/> representing the item's appearance.</returns>
        IItemPart GetOrAddItemPart(int itemId);
        /// <summary>
        /// Caches the appearance information for a specific item ID.
        /// </summary>
        /// <param name="itemId">The ID of the item.</param>
        /// <param name="itemPart">The appearance information to cache.</param>
        void SetItemPart(int itemId, IItemPart itemPart);
        /// <summary>
        /// Sets the color for a specific customizable part of the character's appearance.
        /// </summary>
        /// <param name="type">The type of color to set (e.g., hair, skin).</param>
        /// <param name="value">The new color value.</param>
        void SetColor(ColorType type, int value);
        /// <summary>
        /// Gets the current look or style ID for a specific part of the character's base appearance.
        /// </summary>
        /// <param name="type">The type of look to get (e.g., hairstyle, beard style).</param>
        /// <returns>The ID of the currently set look.</returns>
        int GetLook(LookType type);
        /// <summary>
        /// Sets the look or style for a specific part of the character's base appearance.
        /// </summary>
        /// <param name="type">The type of look to set.</param>
        /// <param name="look">The ID of the new look.</param>
        void SetLook(LookType type, int look);
        /// <summary>
        /// Gets the ID of the item model drawn on a specific body part.
        /// </summary>
        /// <param name="part">The body part to check.</param>
        /// <returns>The item model ID, or 0 if no item is drawn.</returns>
        int GetDrawnBodyPart(BodyPart part);
        /// <summary>
        /// Applies the character's base appearance (looks and colors) to their model.
        /// </summary>
        void DrawCharacter();
        /// <summary>
        /// Draws a specified item on a body part.
        /// </summary>
        /// <param name="part">The body part on which to draw the item.</param>
        /// <param name="itemId">The ID of the item to draw.</param>
        void DrawItem(BodyPart part, int itemId);
        /// <summary>
        /// Forces a refresh of the character's appearance, flagging it for a client-side update.
        /// </summary>
        void Refresh();
        /// <summary>
        /// Resets the character's render ID, forcing a recalculation on the next update.
        /// </summary>
        void ResetRenderID();
    }
}
