using System;
using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    public class NpcTypeCodec : INpcTypeCodec
    {
        public INpcType Decode(int id, MemoryStream stream)
        {
            var npcType = new NpcType(id);
            Decode(npcType, stream);
            return npcType;
        }

        public MemoryStream Encode(INpcType npcType)
        {
            var npc = (NpcType)npcType;
            var writer = new MemoryStream();

            if (npc.ModelIDs != null)
            {
                writer.WriteByte(1);
                writer.WriteByte((byte)npc.ModelIDs.Length);
                foreach (var modelId in npc.ModelIDs)
                {
                    writer.WriteBigSmart(modelId);
                }
            }

            if (npc.Name != "null")
            {
                writer.WriteByte(2);
                writer.WriteString(npc.Name);
            }

            if (npc.Size != 1)
            {
                writer.WriteByte(12);
                writer.WriteByte((byte)npc.Size);
            }

            for (int i = 0; i < 5; i++)
            {
                if (npc.Actions[i] != null && npc.Actions[i] != "Attack")
                {
                    writer.WriteByte((byte)(30 + i));
                    writer.WriteString(npc.Actions[i]);
                }
            }

            if (npc.OriginalColours != null && npc.ModifiedColours != null)
            {
                writer.WriteByte(40);
                writer.WriteByte((byte)npc.OriginalColours.Length);
                for (int i = 0; i < npc.OriginalColours.Length; i++)
                {
                    writer.WriteShort(npc.OriginalColours[i]);
                    writer.WriteShort(npc.ModifiedColours[i]);
                }
            }

            if (npc.AShortArray1508 != null && npc.AShortArray1491 != null)
            {
                writer.WriteByte(41);
                writer.WriteByte((byte)npc.AShortArray1508.Length);
                for (int i = 0; i < npc.AShortArray1508.Length; i++)
                {
                    writer.WriteShort(npc.AShortArray1508[i]);
                    writer.WriteShort(npc.AShortArray1491[i]);
                }
            }

            if (npc.AByteArray1446 != null)
            {
                writer.WriteByte(42);
                writer.WriteByte((byte)npc.AByteArray1446.Length);
                foreach (var val in npc.AByteArray1446)
                {
                    writer.WriteByte((byte)val);
                }
            }

            if (npc.InterfaceModelIDs != null)
            {
                writer.WriteByte(60);
                writer.WriteByte((byte)npc.InterfaceModelIDs.Length);
                foreach (var modelId in npc.InterfaceModelIDs)
                {
                    writer.WriteBigSmart(modelId);
                }
            }

            if (!npc.VisibleOnMinimap)
            {
                writer.WriteByte(93);
            }

            if (npc.CombatLevel != -1)
            {
                writer.WriteByte(95);
                writer.WriteShort(npc.CombatLevel);
            }

            if (npc.ScaleX != 128)
            {
                writer.WriteByte(97);
                writer.WriteShort(npc.ScaleX);
            }

            if (npc.ScaleY != 128)
            {
                writer.WriteByte(98);
                writer.WriteShort(npc.ScaleY);
            }

            if (npc.IsVisible)
            {
                writer.WriteByte(99);
            }

            if (npc.LightModifier != 0)
            {
                writer.WriteByte(100);
                writer.WriteByte((byte)npc.LightModifier);
            }

            if (npc.ShadowModifier != 0)
            {
                writer.WriteByte(101);
                writer.WriteByte((byte)(npc.ShadowModifier / 5));
            }

            if (npc.HeadIcon != -1)
            {
                writer.WriteByte(102);
                writer.WriteShort(npc.HeadIcon);
            }

            if (npc.DegreesToTurn != 32)
            {
                writer.WriteByte(103);
                writer.WriteShort(npc.DegreesToTurn);
            }

            if (npc.TransformToIDs != null)
            {
                writer.WriteByte(118); // or 106
                writer.WriteShort(npc.VarpBitFileID);
                writer.WriteShort(npc.ConfigID);
                var lastVal = npc.TransformToIDs[npc.TransformToIDs.Length - 1];
                writer.WriteShort(lastVal);
                writer.WriteByte((byte)(npc.TransformToIDs.Length - 2));
                for (int i = 0; i <= npc.TransformToIDs.Length - 2; i++)
                {
                    writer.WriteShort(npc.TransformToIDs[i]);
                }
            }

            if (!npc.IsClickable)
            {
                writer.WriteByte(107);
            }

            if (!npc.ABoolean1470)
            {
                writer.WriteByte(109);
            }

            if (!npc.ABoolean1457)
            {
                writer.WriteByte(111);
            }

            if (npc.AShort1495 != 0 || npc.AShort1490 != 0)
            {
                writer.WriteByte(113);
                writer.WriteShort(npc.AShort1495);
                writer.WriteShort(npc.AShort1490);
            }

            if (npc.AByte1447 != -96 || npc.AByte1445 != -16)
            {
                writer.WriteByte(114);
                writer.WriteByte((byte)npc.AByte1447);
                writer.WriteByte((byte)npc.AByte1445);
            }

            if (npc.WalkingProperties != 0)
            {
                writer.WriteByte(119);
                writer.WriteByte((byte)npc.WalkingProperties);
            }

            if (npc.AnIntArrayArray1449 != null && npc.ModelIDs != null)
            {
                writer.WriteByte(121);
                writer.WriteByte((byte)npc.AnIntArrayArray1449.Length);
                for(int i = 0; i < npc.AnIntArrayArray1449.Length; i++)
                {
                    for(int j = 0; j < npc.AnIntArrayArray1449[i].Length; j++)
                    {
                        writer.WriteByte((byte)npc.AnIntArrayArray1449[i][j]);
                    }
                }
            }

            if (npc.AnInt1462 != -1)
            {
                writer.WriteByte(123);
                writer.WriteShort(npc.AnInt1462);
            }

            if (npc.SpawnFaceDirection != 7)
            {
                writer.WriteByte(125);
                writer.WriteByte((byte)npc.SpawnFaceDirection);
            }

            if (npc.RenderId != -1)
            {
                writer.WriteByte(127);
                writer.WriteShort(npc.RenderId);
            }

            if (npc.Speed != 0)
            {
                writer.WriteByte(128);
                writer.WriteByte((byte)npc.Speed);
            }

            if (npc.IdleAnimationId != -1 || npc.MoveType1AnimationId != -1 || npc.MoveType2AnimationId != -1 || npc.RunAnimationId != -1)
            {
                writer.WriteByte(134);
                writer.WriteShort(npc.IdleAnimationId);
                writer.WriteShort(npc.MoveType1AnimationId);
                writer.WriteShort(npc.MoveType2AnimationId);
                writer.WriteShort(npc.RunAnimationId);
                writer.WriteByte(npc.AnInt1504);
            }

            if (npc.AnInt1480 != -1 || npc.AnInt1453 != -1)
            {
                writer.WriteByte(135);
                writer.WriteByte((byte)npc.AnInt1480);
                writer.WriteShort(npc.AnInt1453);
            }

            if (npc.AnInt1510 != -1 || npc.AnInt1475 != -1)
            {
                writer.WriteByte(136);
                writer.WriteByte((byte)npc.AnInt1510);
                writer.WriteShort(npc.AnInt1475);
            }

            if (npc.AttackCursor != -1)
            {
                writer.WriteByte(137);
                writer.WriteShort(npc.AttackCursor);
            }

            if (npc.AnInt1507 != -1)
            {
                writer.WriteByte(138);
                writer.WriteBigSmart(npc.AnInt1507);
            }

            if (npc.AnInt1456 != 255)
            {
                writer.WriteByte(140);
                writer.WriteByte((byte)npc.AnInt1456);
            }

            if (npc.ABoolean1511)
            {
                writer.WriteByte(141);
            }

            if (npc.MapIcon != -1)
            {
                writer.WriteByte(142);
                writer.WriteShort(npc.MapIcon);
            }

            if (npc.ABoolean1483)
            {
                writer.WriteByte(143);
            }

            for (int i = 0; i < 5; i++)
            {
                if (npc.Actions[i] != null && npc.Actions[i] == "Attack")
                {
                    writer.WriteByte((byte)(150 + i));
                    writer.WriteString(npc.Actions[i]);
                }
            }

            if (npc.ModelRedColor != 0 || npc.ModelGreenColor != 0 || npc.ModelBlueColor != 0 || npc.ModelAlphaColor != 0)
            {
                writer.WriteByte(155);
                writer.WriteByte((byte)npc.ModelRedColor);
                writer.WriteByte((byte)npc.ModelGreenColor);
                writer.WriteByte((byte)npc.ModelBlueColor);
                writer.WriteByte((byte)npc.ModelAlphaColor);
            }

            if (npc.AByte1487 == 1)
            {
                writer.WriteByte(158);
            }

            if (npc.AByte1487 == 0)
            {
                writer.WriteByte(159);
            }

            if (npc.QuestIDs != null)
            {
                writer.WriteByte(160);
                writer.WriteByte((byte)npc.QuestIDs.Length);
                foreach (var questId in npc.QuestIDs)
                {
                    writer.WriteShort(questId);
                }
            }

            if (npc.HasDisplayName)
            {
                writer.WriteByte(162);
            }

            if (npc.AnInt1454 != -1)
            {
                writer.WriteByte(163);
                writer.WriteByte((byte)npc.AnInt1454);
            }

            if (npc.AnInt1502 != 256 || npc.AnInt1463 != 256)
            {
                writer.WriteByte(164);
                writer.WriteShort(npc.AnInt1502);
                writer.WriteShort(npc.AnInt1463);
            }

            if (npc.AnInt1497 != 0)
            {
                writer.WriteByte(165);
                writer.WriteByte((byte)npc.AnInt1497);
            }

            if (npc.AnInt1464 != 0)
            {
                writer.WriteByte(168);
                writer.WriteByte((byte)npc.AnInt1464);
            }

            if (npc.ExtraData != null)
            {
                writer.WriteByte(249);
                writer.WriteByte((byte)npc.ExtraData.Count);
                foreach (var pair in npc.ExtraData)
                {
                    writer.WriteByte((byte)(pair.Value is string ? 1 : 0));
                    writer.WriteMedInt(pair.Key);
                    if (pair.Value is string s)
                    {
                        writer.WriteString(s);
                    }
                    else
                    {
                        writer.WriteInt((int)pair.Value);
                    }
                }
            }


            writer.WriteByte(0);
            return writer;
        }

        private void Decode(NpcType npcType, MemoryStream stream)
        {
            while (true)
            {
                int opcode = stream.ReadUnsignedByte();
                if (opcode == 0)
                {
                    return;
                }
                if (opcode != 1)
                {
                    if (opcode == 2)
                        npcType.Name = stream.ReadString();
                    else if (opcode != 12)
                    {
                        if (opcode >= 30 && opcode < 35)
                            npcType.Actions[opcode - 30] = stream.ReadString();
                        else if (opcode == 40)
                        {
                            int len = stream.ReadUnsignedByte();
                            npcType.ModifiedColours = new short[len];
                            npcType.OriginalColours = new short[len];
                            for (int i = 0; len > i; i++)
                            {
                                npcType.OriginalColours[i] = (short)stream.ReadUnsignedShort();
                                npcType.ModifiedColours[i] = (short)stream.ReadUnsignedShort();
                            }
                        }
                        else if (opcode != 41)
                        {
                            if (opcode == 42)
                            {
                                int i4 = stream.ReadUnsignedByte();
                                npcType.AByteArray1446 = new sbyte[i4];
                                for (int i5 = 0; i4 > i5; i5++)
                                    npcType.AByteArray1446[i5] = (sbyte)stream.ReadSignedByte();
                            }
                            else if (opcode == 44)
                            {
                                int i152 = stream.ReadUnsignedShort();
                                /*int i_153_ = 0;
                                for (int i_154_ = i_152_; i_154_ > 0; i_154_ >>= 1)
                                    i_153_++;
                                sbyte[] aByteArray4855 = new sbyte[i_153_];
                                sbyte i_155_ = 0;
                                for (int i_156_ = 0; i_156_ < i_153_; i_156_++)
                                {
                                    if ((i_152_ & 1 << i_156_) > 0)
                                    {
                                        aByteArray4855[i_156_] = i_155_;
                                        i_155_++;
                                    }
                                    else
                                        aByteArray4855[i_156_] = (sbyte)-1;
                                }*/
                            }
                            else if (opcode == 45)
                            {
                                int i157 = stream.ReadUnsignedShort();
                                /*int i_158_ = 0;
                                for (int i_159_ = i_157_; i_159_ > 0; i_159_ >>= 1)
                                    i_158_++;
                                sbyte[] aByteArray4856 = new sbyte[i_158_];
                                sbyte i_160_ = 0;
                                for (int i_161_ = 0; i_161_ < i_158_; i_161_++)
                                {
                                    if ((i_157_ & 1 << i_161_) > 0)
                                    {
                                        aByteArray4856[i_161_] = i_160_;
                                        i_160_++;
                                    }
                                    else
                                        aByteArray4856[i_161_] = (sbyte)-1;
                                }*/
                            }
                            else if (opcode == 60)
                            {
                                int i6 = stream.ReadUnsignedByte();
                                npcType.InterfaceModelIDs = new int[i6];
                                for (int i7 = 0; i6 > i7; i7++)
                                    npcType.InterfaceModelIDs[i7] = stream.ReadBigSmart();
                            }
                            else if (opcode == 93)
                                npcType.VisibleOnMinimap = false;
                            else if (opcode != 95)
                            {
                                if (opcode == 97)
                                    npcType.ScaleX = stream.ReadUnsignedShort();
                                else if (opcode != 98)
                                {
                                    if (opcode != 99)
                                    {
                                        if (opcode != 100)
                                        {
                                            if (opcode == 101)
                                                npcType.ShadowModifier = (sbyte)stream.ReadSignedByte() * 5;
                                            else if (opcode == 102)
                                                npcType.HeadIcon = stream.ReadUnsignedShort();
                                            else if (opcode != 103)
                                            {
                                                if (opcode == 106 || opcode == 118)
                                                {
                                                    npcType.VarpBitFileID = stream.ReadUnsignedShort();
                                                    if (npcType.VarpBitFileID == 65535)
                                                        npcType.VarpBitFileID = -1;
                                                    npcType.ConfigID = stream.ReadUnsignedShort();
                                                    if (npcType.ConfigID == 65535)
                                                        npcType.ConfigID = -1;
                                                    int baseModelId = -1;
                                                    if (opcode == 118)
                                                    {
                                                        baseModelId = stream.ReadUnsignedShort();
                                                        if (baseModelId == 65535)
                                                            baseModelId = -1;
                                                    }

                                                    int baseModelIndex = stream.ReadUnsignedByte();
                                                    npcType.TransformToIDs = new int[baseModelIndex + 2];
                                                    for (int i10 = 0; baseModelIndex >= i10; i10++)
                                                    {
                                                        npcType.TransformToIDs[i10] = (stream.ReadUnsignedShort());
                                                        if (npcType.TransformToIDs[i10] == 65535)
                                                            npcType.TransformToIDs[i10] = -1;
                                                    }

                                                    npcType.TransformToIDs[baseModelIndex + 1] = baseModelId;
                                                }
                                                else if (opcode != 107)
                                                {
                                                    if (opcode == 109)
                                                        npcType.ABoolean1470 = false;
                                                    else if (opcode == 111)
                                                        npcType.ABoolean1457 = false;
                                                    else if (opcode != 113)
                                                    {
                                                        if (opcode != 114)
                                                        {
                                                            if (opcode == 119)
                                                                npcType.WalkingProperties = ((sbyte)stream.ReadSignedByte());
                                                            else if (opcode == 121)
                                                            {
                                                                npcType.AnIntArrayArray1449 = (new int[npcType.ModelIDs.Length][]);
                                                                int i11 = (stream.ReadUnsignedByte());
                                                                for (int i12 = 0; i12 < i11; i12++)
                                                                {
                                                                    int i13 = (stream.ReadUnsignedByte());
                                                                    int[] isa = (npcType.AnIntArrayArray1449[i13] = (new int[3]));
                                                                    isa[0] = ((sbyte)stream.ReadSignedByte());
                                                                    isa[1] = ((sbyte)stream.ReadSignedByte());
                                                                    isa[2] = ((sbyte)stream.ReadSignedByte());
                                                                }
                                                            }
                                                            else if (opcode != 122)
                                                            {
                                                                if (opcode != 123)
                                                                {
                                                                    if (opcode != 125)
                                                                    {
                                                                        if (opcode == 127)
                                                                            npcType.RenderId = (stream.ReadUnsignedShort());
                                                                        else if (opcode != 128)
                                                                        {
                                                                            if (opcode == 134)
                                                                            {
                                                                                // passive anims
                                                                                npcType.IdleAnimationId = stream.ReadUnsignedShort();
                                                                                if (npcType.IdleAnimationId == 65535)
                                                                                    npcType.IdleAnimationId = -1;
                                                                                npcType.MoveType1AnimationId = stream.ReadUnsignedShort();
                                                                                if (npcType.MoveType1AnimationId == 65535)
                                                                                    npcType.MoveType1AnimationId = -1;
                                                                                npcType.MoveType2AnimationId = stream.ReadUnsignedShort();
                                                                                if (npcType.MoveType2AnimationId == 65535)
                                                                                    npcType.MoveType2AnimationId = -1;
                                                                                npcType.RunAnimationId = stream.ReadUnsignedShort();
                                                                                if (npcType.RunAnimationId == 65535)
                                                                                    npcType.RunAnimationId = -1;
                                                                                npcType.AnInt1504 = stream.ReadUnsignedByte();
                                                                            }
                                                                            else if (opcode == 135)
                                                                            {
                                                                                npcType.AnInt1480 = stream.ReadUnsignedByte();
                                                                                npcType.AnInt1453 = stream.ReadUnsignedShort();
                                                                            }
                                                                            else if (opcode == 136)
                                                                            {
                                                                                npcType.AnInt1510 = stream.ReadUnsignedByte();
                                                                                npcType.AnInt1475 = stream.ReadUnsignedShort();
                                                                            }
                                                                            else if (opcode != 137)
                                                                            {
                                                                                if (opcode != 138)
                                                                                {
                                                                                    if (opcode == 140)
                                                                                        npcType.AnInt1456 = stream.ReadUnsignedByte(); // passive anim
                                                                                    else if (opcode != 141)
                                                                                    {
                                                                                        if (opcode != 142)
                                                                                        {
                                                                                            if (opcode == 143)
                                                                                                npcType.ABoolean1483 = true;
                                                                                            else if (opcode >= 150 && opcode < 155)
                                                                                            {
                                                                                                npcType.Actions[opcode - 150] = stream.ReadString();
                                                                                            }
                                                                                            else if (opcode == 155)
                                                                                            {
                                                                                                npcType.ModelRedColor = (sbyte)stream.ReadSignedByte();
                                                                                                npcType.ModelGreenColor = (sbyte)stream.ReadSignedByte();
                                                                                                npcType.ModelBlueColor = (sbyte)stream.ReadSignedByte();
                                                                                                npcType.ModelAlphaColor = (sbyte)stream.ReadSignedByte();
                                                                                            }
                                                                                            else if (opcode == 158)
                                                                                                npcType.AByte1487 = 1;
                                                                                            else if (opcode != 159)
                                                                                            {
                                                                                                if (opcode != 160)
                                                                                                {
                                                                                                    if (opcode == 162)
                                                                                                        npcType.HasDisplayName = true;
                                                                                                    else if (opcode != 163)
                                                                                                    {
                                                                                                        if (opcode == 164)
                                                                                                        {
                                                                                                            npcType.AnInt1502 = stream.ReadUnsignedShort();
                                                                                                            npcType.AnInt1463 = stream.ReadUnsignedShort();
                                                                                                        }
                                                                                                        else if (opcode == 165)
                                                                                                            npcType.AnInt1497 = stream.ReadUnsignedByte();
                                                                                                        else if (opcode != 168)
                                                                                                        {
                                                                                                            if (opcode == 169)
                                                                                                            {
                                                                                                                // some bool = false
                                                                                                            }
                                                                                                            else if (opcode >= 170 && opcode < 176)
                                                                                                            {
                                                                                                                int unknownShort = stream.ReadUnsignedShort();
                                                                                                                if (unknownShort == 65535)
                                                                                                                    unknownShort = -1;
                                                                                                            }
                                                                                                            else if (opcode == 249)
                                                                                                            {
                                                                                                                int i14 = stream.ReadUnsignedByte();
                                                                                                                npcType.ExtraData = new Dictionary<int, object>(i14);
                                                                                                                for (int i16 = 0; i14 > i16; i16++)
                                                                                                                {
                                                                                                                    bool stringValue = stream.ReadUnsignedByte() == 1;
                                                                                                                    int key = stream.ReadMedInt();

                                                                                                                    if (npcType.ExtraData.ContainsKey(key))
                                                                                                                        npcType.ExtraData.Remove(key);

                                                                                                                    if (stringValue)
                                                                                                                        npcType.ExtraData.Add(key, stream.ReadString());
                                                                                                                    else
                                                                                                                        npcType.ExtraData.Add(key, stream.ReadInt());
                                                                                                                }
                                                                                                            }
                                                                                                        }
                                                                                                        else
                                                                                                            npcType.AnInt1464 = stream.ReadUnsignedByte();
                                                                                                    }
                                                                                                    else
                                                                                                        npcType.AnInt1454 = stream.ReadUnsignedByte();
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    int i18 = stream.ReadUnsignedByte();
                                                                                                    npcType.QuestIDs = new int[i18];
                                                                                                    for (int i19 = 0; i19 < i18; i19++)
                                                                                                        npcType.QuestIDs[i19] = stream.ReadUnsignedShort();
                                                                                                }
                                                                                            }
                                                                                            else
                                                                                                npcType.AByte1487 = 0;
                                                                                        }
                                                                                        else
                                                                                            npcType.MapIcon = stream.ReadUnsignedShort();
                                                                                    }
                                                                                    else
                                                                                        npcType.ABoolean1511 = true;
                                                                                }
                                                                                else
                                                                                    npcType.AnInt1507 = stream.ReadBigSmart();
                                                                            }
                                                                            else
                                                                                npcType.AttackCursor = stream.ReadUnsignedShort();
                                                                        }
                                                                        else
                                                                            npcType.Speed = stream.ReadUnsignedByte();
                                                                    }
                                                                    else
                                                                        npcType.SpawnFaceDirection = ((sbyte)stream.ReadSignedByte());
                                                                }
                                                                else
                                                                    npcType.AnInt1462 = (stream.ReadUnsignedShort());
                                                            }
                                                            else
                                                                npcType.AnInt1485 = 0; // not used
                                                        }
                                                        else
                                                        {
                                                            npcType.AByte1447 = (sbyte)stream.ReadSignedByte();
                                                            npcType.AByte1445 = (sbyte)stream.ReadSignedByte();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        npcType.AShort1495 = (short)(stream.ReadUnsignedShort());
                                                        npcType.AShort1490 = (short)(stream.ReadUnsignedShort());
                                                    }
                                                }
                                                else
                                                    npcType.IsClickable = false;
                                            }
                                            else
                                                npcType.DegreesToTurn = stream.ReadUnsignedShort();
                                        }
                                        else
                                            npcType.LightModifier = stream.ReadSignedByte();
                                    }
                                    else
                                        npcType.IsVisible = true;
                                }
                                else
                                    npcType.ScaleY = stream.ReadUnsignedShort();
                            }
                            else
                                npcType.CombatLevel = stream.ReadUnsignedShort();
                        }
                        else
                        {
                            int i20 = stream.ReadUnsignedByte();
                            npcType.AShortArray1508 = new short[i20];
                            npcType.AShortArray1491 = new short[i20];
                            for (int i21 = 0; i20 > i21; i21++)
                            {
                                npcType.AShortArray1508[i21] = (short)stream.ReadUnsignedShort();
                                npcType.AShortArray1491[i21] = (short)stream.ReadUnsignedShort();
                            }
                        }
                    }
                    else
                        npcType.Size = stream.ReadUnsignedByte();
                }
                else
                {
                    int modelCount = stream.ReadUnsignedByte();
                    npcType.ModelIDs = new int[modelCount];
                    for (int index = 0; modelCount > index; index++)
                    {
                        npcType.ModelIDs[index] = stream.ReadBigSmart();
                        if (npcType.ModelIDs[index] == 65535)
                            npcType.ModelIDs[index] = -1;
                    }
                }
            }
        }
    }
}