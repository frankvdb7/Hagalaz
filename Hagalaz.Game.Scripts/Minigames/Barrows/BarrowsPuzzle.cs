using System;
using System.Linq;

namespace Hagalaz.Game.Scripts.Minigames.Barrows
{
    /// <summary>
    ///     The centre-door puzzle: which chest-room door is operable, which shapes are shown and which choice is correct.
    ///     Model ids, interface layout and door slots come from the pinned 742 cache (docs/revision-742/reference/minigames.md);
    ///     the bars and polygons answers and the locked-door behaviour rest on cited sources, not on a live client.
    /// </summary>
    internal static class BarrowsPuzzle
    {
        public const int InterfaceId = 25;

        /// <summary>Choice button components, left to right.</summary>
        public static readonly int[] ChoiceComponents = [2, 3, 5];

        /// <summary>Sequence model components.</summary>
        public static readonly int[] SequenceComponents = [6, 7, 8];

        /// <summary>
        ///     Door slots (config 452 bit 10 + slot) of the four doors around the chest.
        /// </summary>
        public static readonly int[] ChestDoorSlots = [4, 8, 9, 11];

        public const int PuzzleCount = 4;

        // Per puzzle: three sequence models, then three choice models; the first choice is the correct one.
        private static readonly int[][] Models =
        [
            [6716, 6717, 6718, 6713, 6714, 6715], // arrows
            [6722, 6723, 6724, 6719, 6720, 6721], // squares
            [6728, 6729, 6730, 6725, 6726, 6727], // bars
            [6734, 6735, 6736, 6731, 6732, 6733] // polygons
        ];

        public static int[] SequenceModels(int puzzleIndex) => Models[puzzleIndex][..3];

        /// <summary>
        ///     Returns the choice models in button order plus the button index (0..2) holding the correct answer.
        /// </summary>
        public static (int[] Choices, int CorrectIndex) Choices(int puzzleIndex, Func<int, int> next)
        {
            var choices = Models[puzzleIndex][3..];
            var correct = choices[0];
            for (var i = choices.Length - 1; i > 0; i--)
            {
                var j = next(i + 1);
                (choices[i], choices[j]) = (choices[j], choices[i]);
            }

            return (choices, Array.IndexOf(choices, correct));
        }

        /// <summary>
        ///     Returns the index into <see cref="ChestDoorSlots" /> for a door object id, or -1 when it is not a chest-room door.
        ///     Slot n is the right-leaf object 6716 + n and the left-leaf object 6735 + n.
        /// </summary>
        public static int ChestDoorIndex(int objectId)
        {
            var slot = objectId is >= 6716 and <= 6731 ? objectId - 6716 :
                objectId is >= 6735 and <= 6750 ? objectId - 6735 : -1;
            return Array.IndexOf(ChestDoorSlots, slot);
        }

        /// <summary>
        ///     Config 452 bits that lock every chest-room door except the operable one.
        /// </summary>
        public static int LockedDoorMask(int operableDoorIndex) =>
            ChestDoorSlots.Where((_, index) => index != operableDoorIndex).Aggregate(0, (mask, slot) => mask | 1 << (10 + slot));

        /// <summary>
        ///     Picks a different operable door and a puzzle.
        /// </summary>
        public static (int DoorIndex, int PuzzleIndex) Reroll(int currentDoorIndex, Func<int, int> next)
        {
            var door = (currentDoorIndex + 1 + next(ChestDoorSlots.Length - 1)) % ChestDoorSlots.Length;
            return (door, next(PuzzleCount));
        }
    }
}
