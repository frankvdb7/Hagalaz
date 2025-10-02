using Hagalaz.Cache.Abstractions.Types;
using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    public class ObjectTypeCodec : IObjectTypeCodec
    {
        public IObjectType Decode(int id, MemoryStream stream)
        {
            var objectType = new ObjectType(id);
            Decode(objectType, stream);
            return objectType;
        }

        public MemoryStream Encode(IObjectType type)
        {
            var obj = (ObjectType)type;
            var writer = new MemoryStream();

            if (obj.ModelIDs != null && obj.ModelIDs.Length > 0)
            {
                writer.WriteByte(1);
                writer.WriteByte((byte)obj.ModelIDs.Length);
                for (int i = 0; i < obj.ModelIDs.Length; i++)
                {
                    writer.WriteByte((byte)obj.Shapes[i]);
                    writer.WriteByte((byte)obj.ModelIDs[i].Length);
                    foreach (var modelId in obj.ModelIDs[i])
                    {
                        writer.WriteBigSmart(modelId);
                    }
                }
            }

            if (obj.Name != "null")
            {
                writer.WriteByte(2);
                writer.WriteString(obj.Name);
            }

            if (obj.SizeX != 1)
            {
                writer.WriteByte(14);
                writer.WriteByte((byte)obj.SizeX);
            }

            if (obj.SizeY != 1)
            {
                writer.WriteByte(15);
                writer.WriteByte((byte)obj.SizeY);
            }

            if (obj.ClipType == 0)
            {
                writer.WriteByte(17);
            }
            else if (!obj.Solid)
            {
                writer.WriteByte(18);
            }

            if (obj.Interactable != -1)
            {
                writer.WriteByte(19);
                writer.WriteByte((byte)obj.Interactable);
            }

            if (obj.GroundContoured == 1)
            {
                writer.WriteByte(21);
            }

            if (obj.DelayShading)
            {
                writer.WriteByte(22);
            }

            if (obj.Occludes == 1)
            {
                writer.WriteByte(23);
            }

            if (obj.AnimationIDs != null && obj.AnimationIDs.Length == 1)
            {
                writer.WriteByte(24);
                writer.WriteBigSmart(obj.AnimationIDs[0]);
            }

            if (obj.ClipType == 1)
            {
                writer.WriteByte(27);
            }

            if (obj.DecorDisplacement != 64)
            {
                writer.WriteByte(28);
                writer.WriteByte((byte)(obj.DecorDisplacement >> 2));
            }

            if (obj.Ambient != 0)
            {
                writer.WriteByte(29);
                writer.WriteSignedByte((sbyte)obj.Ambient);
            }

            if (obj.Contrast != 0)
            {
                writer.WriteByte(39);
                writer.WriteSignedByte((sbyte)(obj.Contrast / 5));
            }

            for (int i = 0; i < 5; i++)
            {
                if (obj.Actions[i] != null)
                {
                    writer.WriteByte((byte)(30 + i));
                    writer.WriteString(obj.Actions[i]);
                }
            }

            if (obj.OriginalColors != null && obj.ModifiedColors != null)
            {
                writer.WriteByte(40);
                writer.WriteByte((byte)obj.OriginalColors.Length);
                for(int i = 0; i < obj.OriginalColors.Length; i++)
                {
                    writer.WriteShort(obj.OriginalColors[i]);
                    writer.WriteShort(obj.ModifiedColors[i]);
                }
            }

            if (obj.AShortArray831 != null && obj.AShortArray762 != null)
            {
                writer.WriteByte(41);
                writer.WriteByte((byte)obj.AShortArray831.Length);
                for(int i = 0; i < obj.AShortArray831.Length; i++)
                {
                    writer.WriteShort(obj.AShortArray831[i]);
                    writer.WriteShort(obj.AShortArray762[i]);
                }
            }

            if (obj.AByteArray816 != null)
            {
                writer.WriteByte(42);
                writer.WriteByte((byte)obj.AByteArray816.Length);
                foreach (var val in obj.AByteArray816)
                {
                    writer.WriteSignedByte(val);
                }
            }

            if(obj.Inverted)
            {
                writer.WriteByte(62);
            }

            if (!obj.CastsShadow)
            {
                writer.WriteByte(64);
            }

            if (obj.ScaleX != 128)
            {
                writer.WriteByte(65);
                writer.WriteShort(obj.ScaleX);
            }

            if (obj.ScaleY != 128)
            {
                writer.WriteByte(66);
                writer.WriteShort(obj.ScaleY);
            }

            if (obj.ScaleZ != 128)
            {
                writer.WriteByte(67);
                writer.WriteShort(obj.ScaleZ);
            }

            if (obj.Surroundings != 0)
            {
                writer.WriteByte(69);
                writer.WriteByte(obj.Surroundings);
            }

            if (obj.OffsetX != 0)
            {
                writer.WriteByte(70);
                writer.WriteShort(obj.OffsetX >> 2);
            }

            if (obj.OffsetY != 0)
            {
                writer.WriteByte(71);
                writer.WriteShort(obj.OffsetY >> 2);
            }

            if (obj.OffsetZ != 0)
            {
                writer.WriteByte(72);
                writer.WriteShort(obj.OffsetZ >> 2);
            }

            if (obj.ObstructsGround)
            {
                writer.WriteByte(73);
            }

            if (obj.Gateway)
            {
                writer.WriteByte(74);
            }

            if (obj.SupportItemsFlag != -1)
            {
                writer.WriteByte(75);
                writer.WriteByte((byte)obj.SupportItemsFlag);
            }

            if (obj.AmbientSoundID != -1)
            {
                writer.WriteByte(78);
                writer.WriteShort(obj.AmbientSoundID);
                writer.WriteByte((byte)obj.AmbientSoundHearDistance);
            }

            if (obj.AudioTracks != null)
            {
                writer.WriteByte(79);
                writer.WriteShort(obj.AnInt833);
                writer.WriteShort(obj.AnInt768);
                writer.WriteByte((byte)obj.AmbientSoundHearDistance);
                writer.WriteByte((byte)obj.AudioTracks.Length);
                foreach (var track in obj.AudioTracks)
                {
                    writer.WriteShort(track);
                }
            }

            if (obj.GroundContoured == 2)
            {
                writer.WriteByte(81);
                writer.WriteByte((byte)(obj.AnInt780 / 256));
            }

            if (obj.Hidden)
            {
                writer.WriteByte(82);
            }

            if (!obj.ABoolean779)
            {
                writer.WriteByte(88);
            }

            if (!obj.ABoolean838)
            {
                writer.WriteByte(89);
            }

            if (obj.MembersOnly)
            {
                writer.WriteByte(91);
            }

            if (obj.TransformToIDs != null)
            {
                bool hasExtra = obj.TransformToIDs[obj.TransformToIDs.Length - 1] != -1;
                writer.WriteByte((byte)(hasExtra ? 92 : 77));
                writer.WriteShort(obj.VarpBitFileId);
                writer.WriteShort(obj.VarpFileId);
                if (hasExtra)
                {
                    writer.WriteBigSmart(obj.TransformToIDs[obj.TransformToIDs.Length - 1]);
                }
                writer.WriteByte((byte)(obj.TransformToIDs.Length - 2));
                for(int i = 0; i < obj.TransformToIDs.Length - 1; i++)
                {
                    writer.WriteBigSmart(obj.TransformToIDs[i]);
                }
            }

            if(obj.GroundContoured == 3 && obj.AnInt780 != -1)
            {
                writer.WriteByte(93);
                writer.WriteShort(obj.AnInt780);
            }

            if(obj.GroundContoured == 4)
            {
                writer.WriteByte(94);
            }

            if(obj.GroundContoured == 5 && obj.AnInt780 != -1)
            {
                writer.WriteByte(95);
                writer.WriteShort(obj.AnInt780);
            }

            if (obj.AdjustMapSceneRotation)
            {
                writer.WriteByte(97);
            }

            if (obj.HasAnimation)
            {
                writer.WriteByte(98);
            }

            if(obj.AnInt788 != -1)
            {
                writer.WriteByte(99);
                writer.WriteByte((byte)obj.AnInt788);
                writer.WriteShort(obj.AnInt827);
            }

            if(obj.AnInt764 != -1)
            {
                writer.WriteByte(100);
                writer.WriteByte((byte)obj.AnInt764);
                writer.WriteShort(obj.AnInt828);
            }

            if (obj.MapSpriteRotation != 0)
            {
                writer.WriteByte(101);
                writer.WriteByte((byte)obj.MapSpriteRotation);
            }

            if (obj.MapSpriteType != -1)
            {
                writer.WriteByte(102);
                writer.WriteShort(obj.MapSpriteType);
            }

            if (obj.Occludes == 0)
            {
                writer.WriteByte(103);
            }

            if (obj.AmbientSoundVolume != 255)
            {
                writer.WriteByte(104);
                writer.WriteByte((byte)obj.AmbientSoundVolume);
            }

            if(obj.FlipMapSprite)
            {
                writer.WriteByte(105);
            }

            if (obj.AnimationIDs != null && obj.AnIntArray784 != null)
            {
                writer.WriteByte(106);
                writer.WriteByte((byte)obj.AnimationIDs.Length);
                int total = 0;
                var normalized = new int[obj.AnIntArray784.Length];

                for (int i = 0; i < obj.AnIntArray784.Length; i++)
                {
                    total += obj.AnIntArray784[i];
                }

                for (int i = 0; i < obj.AnIntArray784.Length; i++)
                {
                    normalized[i] = obj.AnIntArray784[i] * 65535 / total;
                }

                for (int i = 0; i < obj.AnimationIDs.Length; i++)
                {
                    writer.WriteBigSmart(obj.AnimationIDs[i] == -1 ? 65535 : obj.AnimationIDs[i]);
                    writer.WriteByte((byte)normalized[i]);
                }
            }

            if (obj.MapIcon != -1)
            {
                writer.WriteByte(107);
                writer.WriteShort(obj.MapIcon);
            }

            for (int i = 0; i < 5; i++)
            {
                if (obj.Actions[i] != null)
                {
                    writer.WriteByte((byte)(150 + i));
                    writer.WriteString(obj.Actions[i]);
                }
            }

            if (obj.QuestIDs != null)
            {
                writer.WriteByte(160);
                writer.WriteByte((byte)obj.QuestIDs.Length);
                foreach (var questId in obj.QuestIDs)
                {
                    writer.WriteShort(questId);
                }
            }

            if (obj.GroundContoured == 3 && obj.AnInt780 != -1)
            {
                writer.WriteByte(162);
                writer.WriteInt(obj.AnInt780);
            }

            if (obj.AByte826 != 0 || obj.AByte790 != 0 || obj.AByte821 != 0 || obj.AByte787 != 0)
            {
                writer.WriteByte(163);
                writer.WriteSignedByte(obj.AByte826);
                writer.WriteSignedByte(obj.AByte790);
                writer.WriteSignedByte(obj.AByte821);
                writer.WriteSignedByte(obj.AByte787);
            }

            if (obj.AnInt782 != 0)
            {
                writer.WriteByte(164);
                writer.WriteShort(obj.AnInt782);
            }

            if (obj.AnInt830 != 0)
            {
                writer.WriteByte(165);
                writer.WriteShort(obj.AnInt830);
            }

            if (obj.AnInt778 != 0)
            {
                writer.WriteByte(166);
                writer.WriteShort(obj.AnInt778);
            }

            if (obj.AnInt776 != 0)
            {
                writer.WriteByte(167);
                writer.WriteShort(obj.AnInt776);
            }

            if (obj.ABoolean810)
            {
                writer.WriteByte(168);
            }

            if (obj.ABoolean781)
            {
                writer.WriteByte(169);
            }

            if (obj.AnInt823 != 960)
            {
                writer.WriteByte(170);
                writer.WriteSmart(obj.AnInt823);
            }

            if (obj.AnInt773 != 0)
            {
                writer.WriteByte(171);
                writer.WriteSmart(obj.AnInt773);
            }

            if (obj.AnInt825 != 256 || obj.AnInt808 != 256)
            {
                writer.WriteByte(173);
                writer.WriteShort(obj.AnInt825);
                writer.WriteShort(obj.AnInt808);
            }

            if (obj.ABoolean835)
            {
                writer.WriteByte(177);
            }

            if (obj.AnInt813 != 0)
            {
                writer.WriteByte(178);
                writer.WriteByte((byte)obj.AnInt813);
            }

            if (obj.ExtraData != null && obj.ExtraData.Count > 0)
            {
                writer.WriteByte(249);
                writer.WriteByte((byte)obj.ExtraData.Count);
                foreach (var pair in obj.ExtraData)
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

        private void Decode(ObjectType objectType, MemoryStream buffer)
        {
            while (true)
            {
                int opcode = buffer.ReadUnsignedByte();
                if (opcode == 0)
                {
                    return;
                }
                if (opcode != 1)
                {
                    if (opcode != 2)
                    {
                        if (opcode != 14)
                        {
                            if (opcode == 15)
                                objectType.SizeY = buffer.ReadUnsignedByte();
                            else if (opcode != 17)
                            {
                                if (opcode == 18)
                                    objectType.Solid = false;
                                else if (opcode != 19)
                                {
                                    if (opcode == 21)
                                        objectType.GroundContoured = 1;
                                    else if (opcode == 22)
                                        objectType.DelayShading = true;
                                    else if (opcode != 23)
                                    {
                                        if (opcode == 24)
                                        {
                                            int animationID = buffer.ReadBigSmart();
                                            if (animationID != 65535)
                                                objectType.AnimationIDs = new int[] { animationID };
                                        }
                                        else if (opcode != 27)
                                        {
                                            if (opcode != 28)
                                            {
                                                if (opcode != 29)
                                                {
                                                    if (opcode != 39)
                                                    {
                                                        if (opcode >= 30 && opcode < 35)
                                                            objectType.Actions[opcode - 30] = (buffer.ReadString());
                                                        else if (opcode != 40)
                                                        {
                                                            if (opcode == 41)
                                                            {
                                                                int i20 = (buffer.ReadUnsignedByte());
                                                                objectType.AShortArray831 = new short[i20];
                                                                objectType.AShortArray762 = new short[i20];
                                                                for (int i21 = 0; i20 > i21; i21++)
                                                                {
                                                                    objectType.AShortArray831[i21] = (short)(buffer.ReadUnsignedShort());
                                                                    objectType.AShortArray762[i21] = (short)(buffer.ReadUnsignedShort());
                                                                }
                                                            }
                                                            else if (opcode != 42)
                                                            {
                                                                if (opcode == 44)
                                                                {
                                                                    int i16 = buffer.ReadUnsignedShort();
                                                                }
                                                                else if (opcode == 45)
                                                                {
                                                                    int i21 = buffer.ReadUnsignedShort();
                                                                }
                                                                else if (opcode == 62)
                                                                    objectType.Inverted = true;
                                                                else if (opcode == 64)
                                                                    objectType.CastsShadow = false;
                                                                else if (opcode != 65)
                                                                {
                                                                    if (opcode == 66)
                                                                        objectType.ScaleY = (buffer.ReadUnsignedShort());
                                                                    else if (opcode == 67)
                                                                        objectType.ScaleZ = (buffer.ReadUnsignedShort());
                                                                    else if (opcode != 69)
                                                                    {
                                                                        if (opcode == 70)
                                                                            objectType.OffsetX = ((buffer.ReadShort()) << 2);
                                                                        else if (opcode != 71)
                                                                        {
                                                                            if (opcode != 72)
                                                                            {
                                                                                if (opcode != 73)
                                                                                {
                                                                                    if (opcode != 74)
                                                                                    {
                                                                                        if (opcode != 75)
                                                                                        {
                                                                                            if (opcode != 77 && opcode != 92)
                                                                                            {
                                                                                                if (opcode != 78)
                                                                                                {
                                                                                                    if (opcode != 79)
                                                                                                    {
                                                                                                        if (opcode != 81)
                                                                                                        {
                                                                                                            if (opcode != 82)
                                                                                                            {
                                                                                                                if (opcode == 88)
                                                                                                                    objectType.ABoolean779 = false;
                                                                                                                else if (opcode != 89)
                                                                                                                {
                                                                                                                    if (opcode != 91)
                                                                                                                    {
                                                                                                                        if (opcode == 93)
                                                                                                                        {
                                                                                                                            objectType.GroundContoured = (sbyte)3;
                                                                                                                            objectType.AnInt780 = buffer.ReadUnsignedShort();
                                                                                                                        }
                                                                                                                        else if (opcode == 94)
                                                                                                                            objectType.GroundContoured = (sbyte)4;
                                                                                                                        else if (opcode != 95)
                                                                                                                        {
                                                                                                                            if (opcode != 97)
                                                                                                                            {
                                                                                                                                if (opcode == 98)
                                                                                                                                    objectType.HasAnimation = true;
                                                                                                                                else if (opcode != 99)
                                                                                                                                {
                                                                                                                                    if (opcode == 100)
                                                                                                                                    {
                                                                                                                                        objectType.AnInt764 = buffer.ReadUnsignedByte();
                                                                                                                                        objectType.AnInt828 = buffer.ReadUnsignedShort();
                                                                                                                                    }
                                                                                                                                    else if (opcode != 101)
                                                                                                                                    {
                                                                                                                                        if (opcode == 102)
                                                                                                                                            objectType.MapSpriteType = buffer.ReadUnsignedShort();
                                                                                                                                        else if (opcode != 103)
                                                                                                                                        {
                                                                                                                                            if (opcode == 104)
                                                                                                                                                objectType.AmbientSoundVolume = buffer.ReadUnsignedByte();
                                                                                                                                            else if (opcode == 105)
                                                                                                                                                objectType.FlipMapSprite = true;
                                                                                                                                            else if (opcode == 106)
                                                                                                                                            {
                                                                                                                                                int i22 = buffer.ReadUnsignedByte();
                                                                                                                                                int i23 = 0;
                                                                                                                                                objectType.AnIntArray784 = new int[i22];
                                                                                                                                                objectType.AnimationIDs = new int[i22];
                                                                                                                                                for (int i24 = 0; i22 > i24; i24++)
                                                                                                                                                {
                                                                                                                                                    objectType.AnimationIDs[i24] = buffer.ReadBigSmart();
                                                                                                                                                    if (objectType.AnimationIDs[i24] == 65535)
                                                                                                                                                        objectType.AnimationIDs[i24] = -1;
                                                                                                                                                    i23 += objectType.AnIntArray784[i24] = buffer.ReadUnsignedByte();
                                                                                                                                                }
                                                                                                                                                for (int i25 = 0; i22 > i25; i25++)
                                                                                                                                                    objectType.AnIntArray784[i25] = objectType.AnIntArray784[i25] * 65535 / i23;
                                                                                                                                            }
                                                                                                                                            else if (opcode != 107)
                                                                                                                                            {
                                                                                                                                                if (opcode < 150 || opcode >= 155)
                                                                                                                                                {
                                                                                                                                                    if (opcode == 160)
                                                                                                                                                    {
                                                                                                                                                        int i26 = buffer.ReadUnsignedByte();
                                                                                                                                                        objectType.QuestIDs = new int[i26];
                                                                                                                                                        for (int i27 = 0; i27 < i26; i27++)
                                                                                                                                                            objectType.QuestIDs[i27] = buffer.ReadUnsignedShort();
                                                                                                                                                    }
                                                                                                                                                    else if (opcode != 162)
                                                                                                                                                    {
                                                                                                                                                        if (opcode == 163)
                                                                                                                                                        {
                                                                                                                                                            objectType.AByte826 = (sbyte)buffer.ReadSignedByte();
                                                                                                                                                            objectType.AByte790 = (sbyte)buffer.ReadSignedByte();
                                                                                                                                                            objectType.AByte821 = (sbyte)buffer.ReadSignedByte();
                                                                                                                                                            objectType.AByte787 = (sbyte)buffer.ReadSignedByte();
                                                                                                                                                        }
                                                                                                                                                        else if (opcode != 164)
                                                                                                                                                        {
                                                                                                                                                            if (opcode == 165)
                                                                                                                                                                objectType.AnInt830 = buffer.ReadShort();
                                                                                                                                                            else if (opcode == 166)
                                                                                                                                                                objectType.AnInt778 = buffer.ReadShort();
                                                                                                                                                            else if (opcode == 167)
                                                                                                                                                                objectType.AnInt776 = buffer.ReadUnsignedShort();
                                                                                                                                                            else if (opcode == 168)
                                                                                                                                                                objectType.ABoolean810 = true;
                                                                                                                                                            else if (opcode == 169)
                                                                                                                                                                objectType.ABoolean781 = true;
                                                                                                                                                            else if (opcode != 170)
                                                                                                                                                            {
                                                                                                                                                                if (opcode != 171)
                                                                                                                                                                {
                                                                                                                                                                    if (opcode == 173)
                                                                                                                                                                    {
                                                                                                                                                                        objectType.AnInt825 = buffer.ReadUnsignedShort();
                                                                                                                                                                        objectType.AnInt808 = buffer.ReadUnsignedShort();
                                                                                                                                                                    }
                                                                                                                                                                    else if (opcode == 177)
                                                                                                                                                                        objectType.ABoolean835 = true;
                                                                                                                                                                    else if (opcode != 178)
                                                                                                                                                                    {
                                                                                                                                                                        if (opcode == 189)
                                                                                                                                                                        {
                                                                                                                                                                        }
                                                                                                                                                                        else if (opcode >= 190 && opcode < 196)
                                                                                                                                                                        {
                                                                                                                                                                            buffer.ReadUnsignedShort();
                                                                                                                                                                        }
                                                                                                                                                                        else if (opcode == 249)
                                                                                                                                                                        {
                                                                                                                                                                            int count = buffer.ReadUnsignedByte();
                                                                                                                                                                            objectType.ExtraData = new Dictionary<int, object>(count);
                                                                                                                                                                            for (int i = 0; count > i; i++)
                                                                                                                                                                            {
                                                                                                                                                                                bool isStr = buffer.ReadUnsignedByte() == 1;
                                                                                                                                                                                int key = buffer.ReadMedInt();
                                                                                                                                                                                if (!isStr)
                                                                                                                                                                                    objectType.ExtraData.Add(key, buffer.ReadInt());
                                                                                                                                                                                else
                                                                                                                                                                                    objectType.ExtraData.Add(key, buffer.ReadString());
                                                                                                                                                                            }
                                                                                                                                                                        }
                                                                                                                                                                    }
                                                                                                                                                                    else
                                                                                                                                                                        objectType.AnInt813 = buffer.ReadUnsignedByte();
                                                                                                                                                                }
                                                                                                                                                                else
                                                                                                                                                                    objectType.AnInt773 = buffer.ReadSmart();
                                                                                                                                                            }
                                                                                                                                                            else
                                                                                                                                                                objectType.AnInt823 = buffer.ReadSmart();
                                                                                                                                                        }
                                                                                                                                                        else
                                                                                                                                                            objectType.AnInt782 = buffer.ReadShort();
                                                                                                                                                    }
                                                                                                                                                    else
                                                                                                                                                    {
                                                                                                                                                        objectType.GroundContoured = (sbyte)3;
                                                                                                                                                        objectType.AnInt780 = buffer.ReadInt();
                                                                                                                                                    }
                                                                                                                                                }
                                                                                                                                                else
                                                                                                                                                {
                                                                                                                                                    objectType.Actions[opcode - 150] = buffer.ReadString();
                                                                                                                                                }
                                                                                                                                            }
                                                                                                                                            else
                                                                                                                                                objectType.MapIcon = buffer.ReadUnsignedShort();
                                                                                                                                        }
                                                                                                                                        else
                                                                                                                                            objectType.Occludes = 0;
                                                                                                                                    }
                                                                                                                                    else
                                                                                                                                        objectType.MapSpriteRotation = buffer.ReadUnsignedByte();
                                                                                                                                }
                                                                                                                                else
                                                                                                                                {
                                                                                                                                    objectType.AnInt788 = buffer.ReadUnsignedByte();
                                                                                                                                    objectType.AnInt827 = buffer.ReadUnsignedShort();
                                                                                                                                }
                                                                                                                            }
                                                                                                                            else
                                                                                                                                objectType.AdjustMapSceneRotation = true;
                                                                                                                        }
                                                                                                                        else
                                                                                                                        {
                                                                                                                            objectType.GroundContoured = (sbyte)5;
                                                                                                                            objectType.AnInt780 = buffer.ReadShort();
                                                                                                                        }
                                                                                                                    }
                                                                                                                    else
                                                                                                                        objectType.MembersOnly = true;
                                                                                                                }
                                                                                                                else
                                                                                                                    objectType.ABoolean838 = false;
                                                                                                            }
                                                                                                            else
                                                                                                                objectType.Hidden = true;
                                                                                                        }
                                                                                                        else
                                                                                                        {
                                                                                                            objectType.GroundContoured = (sbyte)2;
                                                                                                            objectType.AnInt780 = buffer.ReadUnsignedByte() * 256;
                                                                                                        }
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        objectType.AnInt833 = buffer.ReadUnsignedShort();
                                                                                                        objectType.AnInt768 = buffer.ReadUnsignedShort();
                                                                                                        objectType.AmbientSoundHearDistance = buffer.ReadUnsignedByte();
                                                                                                        int i32 = buffer.ReadUnsignedByte();
                                                                                                        objectType.AudioTracks = new int[i32];
                                                                                                        for (int i33 = 0; i32 > i33; i33++)
                                                                                                            objectType.AudioTracks[i33] = buffer.ReadUnsignedShort();
                                                                                                    }
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    objectType.AmbientSoundID = buffer.ReadUnsignedShort();
                                                                                                    objectType.AmbientSoundHearDistance = buffer.ReadUnsignedByte();
                                                                                                }
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                objectType.VarpBitFileId = buffer.ReadUnsignedShort();
                                                                                                if (objectType.VarpBitFileId == 65535)
                                                                                                    objectType.VarpBitFileId = -1;
                                                                                                objectType.VarpFileId = buffer.ReadUnsignedShort();
                                                                                                if (objectType.VarpFileId == 65535)
                                                                                                    objectType.VarpFileId = -1;
                                                                                                int i34 = -1;
                                                                                                if (opcode == 92)
                                                                                                {
                                                                                                    i34 = buffer.ReadBigSmart();
                                                                                                }
                                                                                                int childrenCount = buffer.ReadUnsignedByte();
                                                                                                objectType.TransformToIDs = new int[childrenCount + 2];
                                                                                                for (int childIndex = 0; childIndex <= childrenCount; childIndex++)
                                                                                                {
                                                                                                    objectType.TransformToIDs[childIndex] = buffer.ReadBigSmart();
                                                                                                }
                                                                                                objectType.TransformToIDs[childrenCount + 1] = i34;
                                                                                            }
                                                                                        }
                                                                                        else
                                                                                            objectType.SupportItemsFlag = buffer.ReadUnsignedByte();
                                                                                    }
                                                                                    else
                                                                                        objectType.Gateway = true;
                                                                                }
                                                                                else
                                                                                    objectType.ObstructsGround = true;
                                                                            }
                                                                            else
                                                                                objectType.OffsetZ = buffer.ReadShort() << 2;
                                                                        }
                                                                        else
                                                                            objectType.OffsetY = ((buffer.ReadShort()) << 2);
                                                                    }
                                                                    else
                                                                        objectType.Surroundings = (byte)buffer.ReadUnsignedByte();
                                                                }
                                                                else
                                                                    objectType.ScaleX = (buffer.ReadUnsignedShort());
                                                            }
                                                            else
                                                            {
                                                                int i37 = (buffer.ReadUnsignedByte());
                                                                objectType.AByteArray816 = new sbyte[i37];
                                                                for (int i38 = 0; i37 > i38; i38++)
                                                                    objectType.AByteArray816[i38] = ((sbyte)buffer.ReadSignedByte());
                                                            }
                                                        }
                                                        else
                                                        {
                                                            int i39 = (buffer.ReadUnsignedByte());
                                                            objectType.ModifiedColors = new short[i39];
                                                            objectType.OriginalColors = new short[i39];
                                                            for (int i40 = 0; i39 > i40; i40++)
                                                            {
                                                                objectType.OriginalColors[i40] = (short)(buffer.ReadUnsignedShort());
                                                                objectType.ModifiedColors[i40] = (short)(buffer.ReadUnsignedShort());
                                                            }
                                                        }
                                                    }
                                                    else
                                                        objectType.Contrast = (sbyte)buffer.ReadSignedByte() * 5;
                                                }
                                                else
                                                    objectType.Ambient = (sbyte)buffer.ReadSignedByte();
                                            }
                                            else
                                                objectType.DecorDisplacement = (buffer.ReadUnsignedByte() << 2);
                                        }
                                        else
                                            objectType.ClipType = 1;
                                    }
                                    else
                                        objectType.Occludes = 1;
                                }
                                else
                                    objectType.Interactable = buffer.ReadUnsignedByte();
                            }
                            else
                            {
                                objectType.ClipType = 0;
                                objectType.Solid = false;
                            }
                        }
                        else
                            objectType.SizeX = buffer.ReadUnsignedByte();
                    }
                    else
                        objectType.Name = buffer.ReadString();
                }
                else
                {
                    int i41 = buffer.ReadUnsignedByte();
                    objectType.Shapes = new sbyte[i41];
                    objectType.ModelIDs = new int[i41][];
                    for (int i42 = 0; i41 > i42; i42++)
                    {
                        objectType.Shapes[i42] = (sbyte)buffer.ReadSignedByte();
                        int i43 = buffer.ReadUnsignedByte();
                        objectType.ModelIDs[i42] = new int[i43];
                        for (int i44 = 0; i44 < i43; i44++)
                            objectType.ModelIDs[i42][i44] = buffer.ReadBigSmart();
                    }
                }
            }
        }
    }
}