using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Degraded
{
    /// <summary>
    ///     Contains degraded item equipment script.
    /// </summary>
    [EquipmentScriptMetaData([13910, 13913, 13916, 13919, 13922, 13925, 13928, 13931, 13934, 13937, 13940, 13943, 13946, 13949, 13952, 13960, 13963, 13966, 13969, 13972, 13975, 13978, 13981, 13984, 13987, 13990])]
    public class DegradedEquipment : EquipmentScript
    {
        /// <summary>
        ///     Happens when this item is equipped by specific character.
        ///     By default , this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equipped the item.</param>
        public override void OnEquipped(IItem item, ICharacter character)
        {
            var degrationTicks = item.ExtraData[0];
            if (degrationTicks == -1)
            {
                degrationTicks = GetDegrationTicks(item.Id);
                item.ExtraData[0] = degrationTicks;
            }

            character.QueueTask(new DegradeTask(character, item, (int)degrationTicks));
        }

        /// <summary>
        ///     Gets the degration ticks.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        private static int GetDegrationTicks(int id)
        {
            if (id == 13910 || id == 13913 || id == 13916 || id == 13919 || id == 13922 || id == 13925 || id == 13928 || id == 13931 ||
                id == 13934 || id == 13937 || id == 13940 || id == 13943 || id == 13946 || id == 13949 || id == 13952)
            {
                return 1500; // corrupted ancient equipment (deg) - 15 minutes.
            }

            if (id == 13960 || id == 13963 || id == 13966 || id == 13969 || id == 13972 || id == 13975 || id == 13978 ||
                id == 13981 || id == 13984 || id == 13987 || id == 13990)
            {
                return 3000; // corrupted dragon equipment (deg) - 30 minutes.
            }

            return -1;
        }
    }
}