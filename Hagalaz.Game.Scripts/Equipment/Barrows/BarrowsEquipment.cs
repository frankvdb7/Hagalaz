using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Barrows
{
    /// <summary>
    ///     Owns the rules shared by every Barrows set: complete-set detection and degradation.
    ///     A concrete set supplies its ordered item IDs and applies its own effect state.
    /// </summary>
    public abstract class BarrowsEquipment : DegradingEquipment
    {
        private const int StagesPerPart = 5;
        private const int DegradedTicks = 22500; // 1/4 of 90.000

        /// <summary>
        ///     The set's 20 item IDs, ordered hat, weapon, chest, legs. Each part lists its original
        ///     (undegraded) ID followed by its four degraded IDs.
        /// </summary>
        protected abstract ReadOnlySpan<int> ItemIds { get; }

        /// <summary>
        ///     Determines whether hat, weapon, chest and legs are all pieces of this set, at any degradation stage.
        /// </summary>
        protected bool HasCompleteSet(ICharacter character) =>
            IsSetPiece(character.Equipment[EquipmentSlot.Hat])
            && IsSetPiece(character.Equipment[EquipmentSlot.Weapon])
            && IsSetPiece(character.Equipment[EquipmentSlot.Chest])
            && IsSetPiece(character.Equipment[EquipmentSlot.Legs]);

        /// <summary>
        ///     Gets the degration ticks for one degradable item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public sealed override int GetDegrationTicks(IItem item) => IsOriginalPiece(item.Id) ? 0 : DegradedTicks;

        /// <summary>
        ///     Gets the degration on death factor.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public sealed override double GetDegrationOnDeathFactor(IItem item) => 0.80;

        /// <summary>
        ///     Gets the degraded item identifier. An original piece degrades to the next ID declared for its part;
        ///     every later stage degrades to <c>id + 1</c>.
        /// </summary>
        /// <param name="item">The current.</param>
        /// <returns></returns>
        public sealed override int GetDegradedItemID(IItem item) => IsOriginalPiece(item.Id) ? ItemIds[ItemIds.IndexOf(item.Id) + 1] : item.Id + 1;

        private bool IsSetPiece(IItem? item) => item != null && ItemIds.Contains(item.Id);

        private bool IsOriginalPiece(int itemId)
        {
            var index = ItemIds.IndexOf(itemId);
            return index >= 0 && index % StagesPerPart == 0;
        }
    }
}
