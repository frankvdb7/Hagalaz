using System;
using System.IO;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types.Defaults
{
    /// <summary>
    /// Represends body data definition which contains info
    /// about how much body parts character model has and other misc info.
    /// </summary>
    public class EquipmentDefaults
    {
        /// <summary>
        /// Contains body data.
        /// </summary>
        /// <value>The parts data.</value>
        public byte[] BodySlotData { get; private set; }
        /// <summary>
        /// Gets something with weapon display.
        /// </summary>
        /// <value>Something with weapon display.</value>
        public int MainHandSlot { get; private set; } = -1;
        /// <summary>
        /// Contains weapon data.
        /// </summary>
        /// <value>The weapon data.</value>
        public int[] WeaponData { get; private set; }
        /// <summary>
        /// Contains shield data.
        /// </summary>
        /// <value>The shield data.</value>
        public int[] ShieldData { get; private set; }
        /// <summary>
        /// Gets something with shield display.
        /// </summary>
        /// <value>Something with shield display.</value>
        public int OffHandSlot { get; private set; } = -1;

        /// <summary>
        /// Parse's given opcode.
        /// </summary>
        /// <param name="buffer">Buffer from which extra data should be readed.</param>
        /// <exception cref="Exception">Unknown opcode:" + opcode</exception>
        /// <exception cref="System.Exception"></exception>
        private void Parse(MemoryStream buffer)
        {
            for (;;)
            {
                var opcode = buffer.ReadUnsignedByte();
                if (opcode == 0)
                    break;
                if (opcode == 1)
                {
                    int length = buffer.ReadUnsignedByte();
                    BodySlotData = new byte[length];
                    for (int i = 0; i < BodySlotData.Length; i++)
                        BodySlotData[i] = (byte) buffer.ReadUnsignedByte();
                }
                else if (opcode == 3)
                {
                    OffHandSlot = buffer.ReadUnsignedByte();
                }
                else if (opcode == 4)
                {
                    MainHandSlot = buffer.ReadUnsignedByte();
                }
                else if (opcode == 5)
                {
                    int length = buffer.ReadUnsignedByte();
                    ShieldData = new int[length];
                    for (int i = 0; i < ShieldData.Length; i++)
                        ShieldData[i] = buffer.ReadUnsignedByte();
                }
                else if (opcode == 6)
                {
                    int length = buffer.ReadUnsignedByte();
                    WeaponData = new int[length];
                    for (int i = 0; i < WeaponData.Length; i++)
                        WeaponData[i] = buffer.ReadUnsignedByte();
                }
                else
                    throw new Exception("Unknown opcode:" + opcode);
            }
        }

        /// <summary>
        /// Read's body data from given owner.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <returns>Body data or null incase of fail.</returns>
        public static EquipmentDefaults Read(ICacheAPI cache)
        {
            var data = new EquipmentDefaults();
            using (var container = cache.ReadContainer(28, 6))
            {
                data.Parse(container.Data);
            }
            return data;
        }
    }
}
