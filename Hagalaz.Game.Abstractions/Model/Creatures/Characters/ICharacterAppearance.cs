namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICharacterAppearance : ICreatureAppearance
    {
        /// <summary>
        /// Contains character gender.
        /// </summary>
        bool Female { get; set; }
        /// <summary>
        /// Contains Id of the npc this character is turned to 
        /// or -1 if character is not NPC.
        /// </summary>
        int NpcId { get; }
        /// <summary>
        /// Contains character render Id.
        /// </summary>
        int RenderId { get; set; }
        /// <summary>
        /// Contains this character prayer icon.
        /// </summary>
        PrayerIcon PrayerIcon { get; set; }
        /// <summary>
        /// Contains this character skull icon.
        /// </summary>
        SkullIcon SkullIcon { get; set; }
        /// <summary>
        /// Contains the display title.
        /// </summary>
        DisplayTitle DisplayTitle { get; set; }
        /// <summary>
        /// Gets the PNPC script.
        /// </summary>
        /// <value>
        /// The PNPC script.
        /// </value>
        ICharacterNpcScript PnpcScript { get; }
        /// <summary>
        /// Gets the item part.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <returns></returns>
        IItemPart? GetDrawnItemPart(BodyPart part);
        /// <summary>
        /// Clear's drawing on given body part.
        /// </summary>
        /// <param name="part">Part which should be cleared.</param>
        void ClearDrawnBodyPart(BodyPart part);
        /// <summary>
        /// Draws the item on body.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="itemPart">The item appearance.</param>
        /// <exception cref="System.Exception">
        /// Unaccessible part!
        /// </exception>
        void DrawItem(BodyPart part, IItemPart itemPart);
        /// <summary>
        /// Turn's character into NPC.
        /// </summary>
        /// <param name="npcId">Id of the npc to which this player should be turned to.</param>
        /// <param name="pnpcScript">The PNPC script.</param>
        void TurnToNpc(int npcId, ICharacterNpcScript? pnpcScript = null);
        /// <summary>
        /// Turn's character back into player.
        /// </summary>
        void TurnToPlayer();
        /// <summary>
        /// Get's character current colorID for given type. 
        /// Throws exception if type is wrong.
        /// </summary>
        /// <param name="type">Type of the color.</param>
        /// <returns></returns>
        int GetColor(ColorType type);
        /// <summary>
        /// Gets or adds the item part.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        IItemPart GetOrAddItemPart(int itemId);

        /// <summary>
        /// Sets the item part.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="itemPart"></param>
        void SetItemPart(int itemId, IItemPart itemPart);
        /// <summary>
        /// Set's character color.
        /// </summary>
        /// <param name="type">Type of the color.</param>
        /// <param name="value">Color value.</param>
        void SetColor(ColorType type, int value);
        /// <summary>
        /// Get's character specific type look.
        /// </summary>
        /// <param name="type">Type of the look to get.</param>
        /// <returns>Look type or throws exception if type is invalid.</returns>
        int GetLook(LookType type);
        /// <summary>
        /// Set's character look.
        /// </summary>
        /// <param name="type">Type of the look to set.</param>
        /// <param name="look">Look value which should be set.</param>
        void SetLook(LookType type, int look);
        /// <summary>
        /// Get's body part.
        /// </summary>
        /// <param name="part">Part of the body to get.</param>
        /// <returns></returns>
        int GetDrawnBodyPart(BodyPart part);
        /// <summary>
        /// Draws the character.
        /// </summary>
        void DrawCharacter();
        /// <summary>
        /// Draw's item on character's body part,
        /// throws exception if that body part is not accessible.
        /// </summary>
        /// <param name="part">Part of the body to draw.</param>
        /// <param name="itemId">Id of the item to draw.</param>
        /// <exception cref="System.Exception">
        /// Unaccessible part!
        /// </exception>
        void DrawItem(BodyPart part, int itemId);
        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        void Refresh();
        /// <summary>
        /// Resets the render Id.
        /// </summary>
        void ResetRenderID();
    }
}
