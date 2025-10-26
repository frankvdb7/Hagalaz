using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types.Defaults;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types.Defaults.Codecs
{
    public class EquipmentDefaultsCodec : ITypeCodec<IEquipmentDefaults>
    {
        public IEquipmentDefaults Decode(int id, MemoryStream stream)
        {
            var definition = new EquipmentDefaults();
            for (;;)
            {
                var opcode = stream.ReadUnsignedByte();
                if (opcode == 0)
                    break;

                switch (opcode)
                {
                    case 1:
                    {
                        int length = stream.ReadUnsignedByte();
                        definition.BodySlotData = new byte[length];
                        for (int i = 0; i < definition.BodySlotData.Length; i++)
                            definition.BodySlotData[i] = (byte)stream.ReadUnsignedByte();
                        break;
                    }
                    case 3:
                        definition.OffHandSlot = stream.ReadUnsignedByte();
                        break;
                    case 4:
                        definition.MainHandSlot = stream.ReadUnsignedByte();
                        break;
                    case 5:
                    {
                        int length = stream.ReadUnsignedByte();
                        definition.ShieldData = new int[length];
                        for (int i = 0; i < definition.ShieldData.Length; i++)
                            definition.ShieldData[i] = stream.ReadUnsignedByte();
                        break;
                    }
                    case 6:
                    {
                        int length = stream.ReadUnsignedByte();
                        definition.WeaponData = new int[length];
                        for (int i = 0; i < definition.WeaponData.Length; i++)
                            definition.WeaponData[i] = stream.ReadUnsignedByte();
                        break;
                    }
                }
            }
            return definition;
        }

        public MemoryStream Encode(IEquipmentDefaults definition)
        {
            var stream = new MemoryStream();
            if (definition.BodySlotData != null)
            {
                stream.WriteByte(1);
                stream.WriteByte((byte)definition.BodySlotData.Length);
                foreach (var t in definition.BodySlotData)
                    stream.WriteByte(t);
            }

            if (definition.OffHandSlot != -1)
            {
                stream.WriteByte(3);
                stream.WriteByte((byte)definition.OffHandSlot);
            }

            if (definition.MainHandSlot != -1)
            {
                stream.WriteByte(4);
                stream.WriteByte((byte)definition.MainHandSlot);
            }

            if (definition.ShieldData != null)
            {
                stream.WriteByte(5);
                stream.WriteByte((byte)definition.ShieldData.Length);
                foreach (var t in definition.ShieldData)
                    stream.WriteByte((byte)t);
            }

            if (definition.WeaponData != null)
            {
                stream.WriteByte(6);
                stream.WriteByte((byte)definition.WeaponData.Length);
                foreach (var t in definition.WeaponData)
                    stream.WriteByte((byte)t);
            }

            stream.WriteByte(0);
            stream.Position = 0;
            return stream;
        }
    }
}
