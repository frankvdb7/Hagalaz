using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// A class for encoding and decoding <see cref="ItemType"/> objects.
    /// </summary>
    public class ItemTypeCodec : IItemTypeCodec
    {
        /// <inheritdoc />
        public IItemType Decode(int id, MemoryStream stream)
        {
            var itemType = new ItemType(id);
            Decode(itemType, stream);
            return itemType;
        }

        /// <inheritdoc />
        public MemoryStream Encode(IItemType type)
        {
            var itemType = (ItemType)type;
            var writer = new MemoryStream();
            writer.WriteByte(1);
            writer.WriteBigSmart(itemType.InterfaceModelId);

            if (!itemType.Name.Equals("null") && itemType.NoteTemplateId == -1)
            {
                writer.WriteByte(2);
                writer.WriteString(itemType.Name);
            }

            if (itemType.ModelZoom != 2000)
            {
                writer.WriteByte(4);
                writer.WriteShort(itemType.ModelZoom);
            }

            if (itemType.ModelRotation1 != 0)
            {
                writer.WriteByte(5);
                writer.WriteShort(itemType.ModelRotation1);
            }

            if (itemType.ModelRotation2 != 0)
            {
                writer.WriteByte(6);
                writer.WriteShort(itemType.ModelRotation2);
            }

            if (itemType.ModelOffset1 != 0)
            {
                writer.WriteByte(7);
                int value = itemType.ModelOffset1;
                if (value < 0)
                    value += 65536;
                writer.WriteShort(value);
            }

            if (itemType.ModelOffset2 != 0)
            {
                writer.WriteByte(8);
                int value = itemType.ModelOffset2;
                if (value < 0)
                    value += 65536;
                writer.WriteShort(value);
            }

            if (itemType.StackableType >= 1 && itemType.NoteTemplateId == -1)
            {
                writer.WriteByte(11);
            }

            if (itemType.MaleHeadModel != -1)
            {
                writer.WriteByte(90);
                writer.WriteShort((ushort)itemType.MaleHeadModel);
            }

            if (itemType.FemaleHeadModel != -1)
            {
                writer.WriteByte(91);
                writer.WriteShort((ushort)itemType.FemaleHeadModel);
            }

            if (itemType.MaleHeadModel2 != -1)
            {
                writer.WriteByte(92);
                writer.WriteShort((ushort)itemType.MaleHeadModel2);
            }

            if (itemType.FemaleHeadModel2 != -1)
            {
                writer.WriteByte(93);
                writer.WriteShort((ushort)itemType.FemaleHeadModel2);
            }

            if (itemType.Zan2D != -1)
            {
                writer.WriteByte(95);
                writer.WriteShort(itemType.Zan2D);
            }

            if (itemType.UnknownInt6 != -1)
            {
                writer.WriteByte(96);
                writer.WriteByte((byte)itemType.UnknownInt6);
            }

            if (itemType.Value != 1 && itemType.LendTemplateId == -1)
            {
                writer.WriteByte(12);
                writer.WriteInt(itemType.Value);
            }

            if (itemType.EquipSlot != -1)
            {
                writer.WriteByte(13);
                writer.WriteByte((byte)itemType.EquipSlot);
            }

            if (itemType.EquipType != -1)
            {
                writer.WriteByte(14);
                writer.WriteByte((byte)itemType.EquipType);
            }

            if (itemType.SomeEquipInt != -1)
            {
                writer.WriteByte(27);
                writer.WriteByte((byte)itemType.SomeEquipInt);
            }

            if (itemType.MembersOnly && itemType.NoteId == -1)
            {
                writer.WriteByte(16);
            }

            if (itemType.MultiStackSize != 0)
            {
                writer.WriteByte(18);
                writer.WriteShort(itemType.MultiStackSize);
            }

            if (itemType.MaleWornModelId1 != -1)
            {
                writer.WriteByte(23);
                writer.WriteBigSmart(itemType.MaleWornModelId1);
            }
            if (itemType.MaleWornModelId2 != -1)
            {
                writer.WriteByte(24);
                writer.WriteBigSmart(itemType.MaleWornModelId2);
            }

            if (itemType.FemaleWornModelId1 != -1)
            {
                writer.WriteByte(25);
                writer.WriteBigSmart(itemType.FemaleWornModelId1);
            }

            if (itemType.FemaleWornModelId2 != -1)
            {
                writer.WriteByte(26);
                writer.WriteBigSmart(itemType.FemaleWornModelId2);
            }

            for (int i = 0; i < itemType.GroundOptions.Length; i++)
            {
                if(itemType.GroundOptions[i] is null || (i == 2 && itemType.GroundOptions[i].ToLower() == "take"))
                    continue;
                writer.WriteByte((byte)(30 + i));
                writer.WriteString(itemType.GroundOptions[i]);
            }

            for (int i = 0; i < itemType.InventoryOptions.Length; i++)
            {
                if(itemType.InventoryOptions[i] is null || (i == 4 && itemType.InventoryOptions[i] == "drop"))
                    continue;
                writer.WriteByte((byte)(35 + i));
                writer.WriteString(itemType.InventoryOptions[i]);
            }

            if (itemType.OriginalModelColors != null && itemType.ModifiedModelColors != null)
            {
                writer.WriteByte(40);
                writer.WriteByte((byte)itemType.OriginalModelColors.Length);
                for (int i = 0; i < itemType.OriginalModelColors.Length; i++)
                {
                    writer.WriteShort(itemType.OriginalModelColors[i]);
                    writer.WriteShort(itemType.ModifiedModelColors[i]);
                }
            }

            if (itemType.OriginalTextureColors != null && itemType.ModifiedTextureColors != null)
            {
                writer.WriteByte(41);
                writer.WriteByte((byte)itemType.OriginalTextureColors.Length);
                for (int i = 0; i < itemType.OriginalTextureColors.Length; i++)
                {
                    writer.WriteShort(itemType.OriginalTextureColors[i]);
                    writer.WriteShort(itemType.ModifiedTextureColors[i]);
                }
            }

            if (itemType.UnknownArray1 != null)
            {
                writer.WriteByte(42);
                writer.WriteByte((byte)itemType.UnknownArray1.Length);
                foreach (var b in itemType.UnknownArray1)
                {
                    writer.WriteSignedByte(b);
                }
            }

            if (itemType.Unnoted)
            {
                writer.WriteByte(65);
            }

            if (itemType.MaleWornModelId3 != -1)
            {
                writer.WriteByte(78);
                writer.WriteBigSmart(itemType.MaleWornModelId3);
            }

            if (itemType.FemaleWornModelId3 != -1)
            {
                writer.WriteByte(79);
                writer.WriteBigSmart(itemType.FemaleWornModelId3);
            }

            if (itemType.NoteId != -1)
            {
                writer.WriteByte(97);
                writer.WriteShort(itemType.NoteId);
            }

            if (itemType.NoteTemplateId != -1)
            {
                writer.WriteByte(98);
                writer.WriteShort(itemType.NoteTemplateId);
            }

            if (itemType.StackIds != null && itemType.StackAmounts != null)
            {
                for (var i = 0; i < itemType.StackIds.Length; i++)
                {
                    if (itemType.StackAmounts[i] == 0)
                        continue;

                    writer.WriteByte((byte)(100 + i));
                    writer.WriteShort(itemType.StackIds[i]);
                    writer.WriteShort(itemType.StackAmounts[i]);
                }
            }

            if (itemType.ScaleX != 128)
            {
                writer.WriteByte(110);
                writer.WriteShort(itemType.ScaleX);
            }

            if (itemType.ScaleY != 128)
            {
                writer.WriteByte(111);
                writer.WriteShort(itemType.ScaleY);
            }

            if (itemType.ScaleZ != 128)
            {
                writer.WriteByte(112);
                writer.WriteShort(itemType.ScaleZ);
            }

            if (itemType.Ambient != 0)
            {
                writer.WriteByte(113);
                writer.WriteSignedByte((sbyte)itemType.Ambient);
            }

            if (itemType.Contrast != 0)
            {
                writer.WriteByte(114);
                writer.WriteSignedByte((sbyte)(itemType.Contrast / 5));
            }

            if (itemType.TeamId != 0)
            {
                writer.WriteByte(115);
                writer.WriteByte((byte)itemType.TeamId);
            }

            if (itemType.LendId != -1)
            {
                writer.WriteByte(121);
                writer.WriteShort(itemType.LendId);
            }

            if (itemType.LendTemplateId != -1)
            {
                writer.WriteByte(122);
                writer.WriteShort(itemType.LendTemplateId);
            }

            if (itemType.MaleWearXOffset != 0 || itemType.MaleWearYOffset != 0 || itemType.MaleWearZOffset != 0)
            {
                writer.WriteByte(125);
                writer.WriteSignedByte((sbyte)(itemType.MaleWearXOffset >> 2));
                writer.WriteSignedByte((sbyte)(itemType.MaleWearYOffset >> 2));
                writer.WriteSignedByte((sbyte)(itemType.MaleWearZOffset >> 2));
            }

            if (itemType.FemaleWearXOffset != 0 || itemType.FemaleWearYOffset != 0 || itemType.FemaleWearZOffset != 0)
            {
                writer.WriteByte(126);
                writer.WriteSignedByte((sbyte)(itemType.FemaleWearXOffset >> 2));
                writer.WriteSignedByte((sbyte)(itemType.FemaleWearYOffset >> 2));
                writer.WriteSignedByte((sbyte)(itemType.FemaleWearZOffset >> 2));
            }

            if (itemType.UnknownInt18 != 0 && itemType.UnknownInt19 != 0)
            {
                writer.WriteByte(127);
                writer.WriteByte((byte)itemType.UnknownInt18);
                writer.WriteShort(itemType.UnknownInt19);
            }

            if (itemType.UnknownInt20 != 0 && itemType.UnknownInt21 != 0)
            {
                writer.WriteByte(128);
                writer.WriteByte((byte)itemType.UnknownInt20);
                writer.WriteShort(itemType.UnknownInt21);
            }

            if (itemType.UnknownInt22 != 0 && itemType.UnknownInt23 != 0)
            {
                writer.WriteByte(129);
                writer.WriteByte((byte)itemType.UnknownInt22);
                writer.WriteShort(itemType.UnknownInt23);
            }

            if (itemType.UnknownInt24 != 0 && itemType.UnknownInt25 != 0)
            {
                writer.WriteByte(130);
                writer.WriteByte((byte)itemType.UnknownInt24);
                writer.WriteShort(itemType.UnknownInt25);
            }

            if (itemType.QuestIDs != null)
            {
                writer.WriteByte(132);
                writer.WriteByte((byte)itemType.QuestIDs.Length);
                foreach (var questId in itemType.QuestIDs)
                {
                    writer.WriteShort(questId);
                }
            }

            if (itemType.PickSizeShift != 0)
            {
                writer.WriteByte(134);
                writer.WriteByte((byte)itemType.PickSizeShift);
            }

            if (itemType.BoughtItemId != -1)
            {
                writer.WriteByte(139);
                writer.WriteShort(itemType.BoughtItemId);
            }

            if (itemType.BoughtTemplateId != -1)
            {
                writer.WriteByte(140);
                writer.WriteShort(itemType.BoughtTemplateId);
            }

            if (itemType.ExtraData != null && itemType.ExtraData.Count > 0)
            {
                writer.WriteByte(249);
                writer.WriteByte((byte)itemType.ExtraData.Count);
                foreach (var pair in itemType.ExtraData)
                {
                    var data = pair.Value;
                    writer.WriteByte((byte)(data is string ? 1 : 0));
                    writer.WriteMedInt(pair.Key);
                    if (data is string s)
                    {
                        writer.WriteString(s);
                    }
                    else
                    {
                        writer.WriteInt((int)data);
                    }
                }
            }
            writer.WriteByte(0);
            return writer;
        }

        private void Decode(ItemType itemType, MemoryStream stream)
        {
            while (true)
            {
                int opcode = stream.ReadUnsignedByte();
                if (opcode == 0)
                {
                    break;
                }
                if (opcode == 1)
                    itemType.InterfaceModelId = stream.ReadBigSmart();
                else if (opcode == 2)
                    itemType.Name = stream.ReadString();
                else if (opcode == 4)
                    itemType.ModelZoom = stream.ReadUnsignedShort();
                else if (opcode == 5)
                    itemType.ModelRotation1 = stream.ReadUnsignedShort();
                else if (opcode == 6)
                    itemType.ModelRotation2 = stream.ReadUnsignedShort();
                else if (opcode == 7)
                {
                    itemType.ModelOffset1 = stream.ReadUnsignedShort();
                    if (itemType.ModelOffset1 > 32767)
                        itemType.ModelOffset1 -= 65536;
                }
                else if (opcode == 8)
                {
                    itemType.ModelOffset2 = stream.ReadUnsignedShort();
                    if (itemType.ModelOffset2 > 32767)
                        itemType.ModelOffset2 -= 65536;
                }
                else if (opcode == 11)
                    itemType.StackableType = 1;
                else if (opcode == 12)
                    itemType.Value = stream.ReadInt();
                else if (opcode == 13)
                    itemType.EquipSlot = (sbyte)stream.ReadUnsignedByte();
                else if (opcode == 14)
                    itemType.EquipType = (sbyte)stream.ReadUnsignedByte();
                else if (opcode == 16)
                    itemType.MembersOnly = true;
                else if (opcode == 18)
                    itemType.MultiStackSize = stream.ReadUnsignedShort();
                else if (opcode == 23)
                    itemType.MaleWornModelId1 = stream.ReadBigSmart();
                else if (opcode == 24)
                    itemType.MaleWornModelId2 = stream.ReadBigSmart();
                else if (opcode == 25)
                    itemType.FemaleWornModelId1 = stream.ReadBigSmart();
                else if (opcode == 26)
                    itemType.FemaleWornModelId2 = stream.ReadBigSmart();
                else if (opcode == 27)
                    itemType.SomeEquipInt = (sbyte)stream.ReadUnsignedByte();
                else if (opcode >= 30 && opcode < 35)
                    itemType.GroundOptions[opcode - 30] = stream.ReadString();
                else if (opcode >= 35 && opcode < 40)
                    itemType.InventoryOptions[opcode - 35] = stream.ReadString();
                else if (opcode == 40)
                {
                    int length = stream.ReadUnsignedByte();
                    itemType.OriginalModelColors = new int[length];
                    itemType.ModifiedModelColors = new int[length];
                    for (int index = 0; index < length; index++)
                    {
                        itemType.OriginalModelColors[index] = stream.ReadUnsignedShort();
                        itemType.ModifiedModelColors[index] = stream.ReadUnsignedShort();
                    }
                }
                else if (opcode == 41)
                {
                    int length = stream.ReadUnsignedByte();
                    itemType.OriginalTextureColors = new int[length];
                    itemType.ModifiedTextureColors = new int[length];
                    for (int index = 0; index < length; index++)
                    {
                        itemType.OriginalTextureColors[index] = stream.ReadUnsignedShort();
                        itemType.ModifiedTextureColors[index] = stream.ReadUnsignedShort();
                    }
                }
                else if (opcode == 42)
                {
                    int length = stream.ReadUnsignedByte();
                    itemType.UnknownArray1 = new sbyte[length];
                    for (int index = 0; index < length; index++)
                        itemType.UnknownArray1[index] = (sbyte)stream.ReadSignedByte();
                }
                else if (opcode == 43)
                {
                    stream.ReadInt();
                }
                else if (opcode == 44)
                {
                    stream.ReadUnsignedShort();
                }
                else if (opcode == 45)
                {
                    stream.ReadUnsignedShort();
                }
                else if (opcode == 65)
                    itemType.Unnoted = true;
                else if (opcode == 78)
                    itemType.MaleWornModelId3 = stream.ReadBigSmart();
                else if (opcode == 79)
                    itemType.FemaleWornModelId3 = stream.ReadBigSmart();
                else if (opcode == 90)
                    itemType.MaleHeadModel = stream.ReadBigSmart();
                else if (opcode == 91)
                    itemType.FemaleHeadModel = stream.ReadBigSmart();
                else if (opcode == 92)
                    itemType.MaleHeadModel2 = stream.ReadBigSmart();
                else if (opcode == 93)
                    itemType.FemaleHeadModel2 = stream.ReadBigSmart();
                else if (opcode == 95)
                    itemType.Zan2D = stream.ReadUnsignedShort();
                else if (opcode == 96)
                    itemType.UnknownInt6 = stream.ReadUnsignedByte();
                else if (opcode == 97)
                    itemType.NoteId = stream.ReadUnsignedShort();
                else if (opcode == 98)
                    itemType.NoteTemplateId = stream.ReadUnsignedShort();
                else if (opcode >= 100 && opcode < 110)
                {
                    if (itemType.StackIds == null || itemType.StackAmounts == null)
                    {
                        itemType.StackIds = new int[10];
                        itemType.StackAmounts = new int[10];
                    }
                    itemType.StackIds[opcode - 100] = stream.ReadUnsignedShort();
                    itemType.StackAmounts[opcode - 100] = stream.ReadUnsignedShort();
                }
                else if (opcode == 110)
                    itemType.ScaleX = stream.ReadUnsignedShort();
                else if (opcode == 111)
                    itemType.ScaleY = stream.ReadUnsignedShort();
                else if (opcode == 112)
                    itemType.ScaleZ = stream.ReadUnsignedShort();
                else if (opcode == 113)
                    itemType.Ambient = (sbyte)stream.ReadSignedByte();
                else if (opcode == 114)
                    itemType.Contrast = (sbyte)stream.ReadSignedByte() * 5;
                else if (opcode == 115)
                    itemType.TeamId = (byte)stream.ReadUnsignedByte();
                else if (opcode == 121)
                    itemType.LendId = stream.ReadUnsignedShort();
                else if (opcode == 122)
                    itemType.LendTemplateId = stream.ReadUnsignedShort();
                else if (opcode == 125)
                {
                    itemType.MaleWearXOffset = (sbyte)stream.ReadSignedByte() << 2;
                    itemType.MaleWearYOffset = (sbyte)stream.ReadSignedByte() << 2;
                    itemType.MaleWearZOffset = (sbyte)stream.ReadSignedByte() << 2;
                }
                else if (opcode == 126)
                {
                    itemType.FemaleWearXOffset = (sbyte)stream.ReadSignedByte() << 2;
                    itemType.FemaleWearYOffset = (sbyte)stream.ReadSignedByte() << 2;
                    itemType.FemaleWearZOffset = (sbyte)stream.ReadSignedByte() << 2;
                }
                else if (opcode == 127)
                {
                    itemType.UnknownInt18 = (byte)stream.ReadUnsignedByte();
                    itemType.UnknownInt19 = stream.ReadUnsignedShort();
                }
                else if (opcode == 128)
                {
                    itemType.UnknownInt20 = (byte)stream.ReadUnsignedByte();
                    itemType.UnknownInt21 = stream.ReadUnsignedShort();
                }
                else if (opcode == 129)
                {
                    itemType.UnknownInt22 = (byte)stream.ReadUnsignedByte();
                    itemType.UnknownInt23 = stream.ReadUnsignedShort();
                }
                else if (opcode == 130)
                {
                    itemType.UnknownInt24 = (byte)stream.ReadUnsignedByte();
                    itemType.UnknownInt25 = stream.ReadUnsignedShort();
                }
                else if (opcode == 132)
                {
                    int length = stream.ReadUnsignedByte();
                    itemType.QuestIDs = new int[length];
                    for (int index = 0; index < length; index++)
                        itemType.QuestIDs[index] = stream.ReadUnsignedShort();
                }
                else if (opcode == 134)
                    itemType.PickSizeShift = stream.ReadUnsignedByte();
                else if (opcode == 139)
                    itemType.BoughtItemId = stream.ReadUnsignedShort();
                else if (opcode == 140)
                    itemType.BoughtTemplateId = stream.ReadUnsignedShort();
                else if (opcode >= 142 && opcode < 147)
                {
                    stream.ReadUnsignedShort();
                }
                else if (opcode >= 150 && opcode < 155)
                {
                    stream.ReadUnsignedShort();
                }
                else if (opcode == 242)
                {
                    stream.ReadBigSmart();
                    stream.ReadBigSmart();
                }
                else if (opcode == 243)
                {
                    stream.ReadBigSmart();
                }
                else if (opcode == 244)
                {
                    stream.ReadBigSmart();
                }
                else if (opcode == 245)
                {
                    stream.ReadBigSmart();
                }
                else if (opcode == 246)
                {
                    stream.ReadBigSmart();
                }
                else if (opcode == 247)
                {
                    stream.ReadBigSmart();
                }
                else if (opcode == 248)
                {
                    stream.ReadBigSmart();
                }
                else if (opcode == 249)
                {
                    var length = stream.ReadUnsignedByte();
                    var extraData = new Dictionary<int, object>(length);
                    for (var index = 0; index < length; index++)
                    {
                        var stringInstance = stream.ReadUnsignedByte() == 1;
                        var key = stream.ReadMedInt();
                        object val;
                        if (stringInstance)
                            val = stream.ReadString();
                        else
                            val = stream.ReadInt();
                        extraData.Remove(key);
                        extraData.Add(key, val);
                    }
                    itemType.ExtraData = extraData;
                }
                else if (opcode == 250)
                {
                    stream.ReadUnsignedByte();
                }
                else if (opcode == 251)
                {
                    int length = stream.ReadUnsignedByte();
                    var oldModelColors = new short[length];
                    var oldModifiedModelColors = new short[length];
                    for (int i = 0; i < length; i++)
                    {
                        oldModelColors[i] = (short)stream.ReadUnsignedShort();
                        oldModifiedModelColors[i] = (short)stream.ReadUnsignedShort();
                    }
                }
                else if (opcode == 252)
                {
                    int length = stream.ReadUnsignedByte();
                    var oldModelTextures = new short[length];
                    var oldModifiedModelTextures = new short[length];
                    for (int i = 0; i < length; i++)
                    {
                        oldModelTextures[i] = (short)stream.ReadUnsignedShort();
                        oldModifiedModelTextures[i] = (short)stream.ReadUnsignedShort();
                    }
                }
                else if (opcode == 253)
                {
                    stream.ReadUnsignedShort();
                    stream.ReadUnsignedShort();
                    stream.ReadUnsignedShort();
                    stream.ReadUnsignedShort();
                }
                else
                {
                    throw new IOException("Unknown opcode:" + opcode);
                }
            }
        }
    }
}