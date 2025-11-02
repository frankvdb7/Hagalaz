using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs
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
                writer.WriteByte(NpcTypeOpcodes.ModelIDs);
                writer.WriteByte((byte)npc.ModelIDs.Length);
                foreach (var modelId in npc.ModelIDs)
                {
                    writer.WriteBigSmart(modelId);
                }
            }

            if (npc.Name != "null")
            {
                writer.WriteByte(NpcTypeOpcodes.Name);
                writer.WriteString(npc.Name);
            }

            if (npc.Size != 1)
            {
                writer.WriteByte(NpcTypeOpcodes.Size);
                writer.WriteByte((byte)npc.Size);
            }

            for (int i = 0; i < 5; i++)
            {
                if (npc.Actions[i] != null && npc.Actions[i] != "Attack")
                {
                    writer.WriteByte((byte)(NpcTypeOpcodes.Actions + i));
                    writer.WriteString(npc.Actions[i]);
                }
            }

            if (npc.OriginalColours != null && npc.ModifiedColours != null && npc.OriginalColours.Length > 0)
            {
                writer.WriteByte(NpcTypeOpcodes.ModifiedColours);
                writer.WriteByte((byte)npc.OriginalColours.Length);
                for (int i = 0; i < npc.OriginalColours.Length; i++)
                {
                    writer.WriteShort(npc.OriginalColours[i]);
                    writer.WriteShort(npc.ModifiedColours[i]);
                }
            }

            if (npc.AShortArray1508 != null && npc.AShortArray1491 != null)
            {
                writer.WriteByte(NpcTypeOpcodes.AShortArrays);
                writer.WriteByte((byte)npc.AShortArray1508.Length);
                for (int i = 0; i < npc.AShortArray1508.Length; i++)
                {
                    writer.WriteShort(npc.AShortArray1508[i]);
                    writer.WriteShort(npc.AShortArray1491[i]);
                }
            }

            if (npc.AByteArray1446 != null)
            {
                writer.WriteByte(NpcTypeOpcodes.AByteArray1446);
                writer.WriteByte((byte)npc.AByteArray1446.Length);
                foreach (var val in npc.AByteArray1446)
                {
                    writer.WriteByte((byte)val);
                }
            }

            if (npc.InterfaceModelIDs != null)
            {
                writer.WriteByte(NpcTypeOpcodes.InterfaceModelIDs);
                writer.WriteByte((byte)npc.InterfaceModelIDs.Length);
                foreach (var modelId in npc.InterfaceModelIDs)
                {
                    writer.WriteBigSmart(modelId);
                }
            }

            if (!npc.VisibleOnMinimap)
            {
                writer.WriteByte(NpcTypeOpcodes.VisibleOnMinimap);
            }

            if (npc.CombatLevel != -1)
            {
                writer.WriteByte(NpcTypeOpcodes.CombatLevel);
                writer.WriteShort(npc.CombatLevel);
            }

            if (npc.ScaleX != 128)
            {
                writer.WriteByte(NpcTypeOpcodes.ScaleX);
                writer.WriteShort(npc.ScaleX);
            }

            if (npc.ScaleY != 128)
            {
                writer.WriteByte(NpcTypeOpcodes.ScaleY);
                writer.WriteShort(npc.ScaleY);
            }

            if (npc.IsVisible)
            {
                writer.WriteByte(NpcTypeOpcodes.IsVisible);
            }

            if (npc.LightModifier != 0)
            {
                writer.WriteByte(NpcTypeOpcodes.LightModifier);
                writer.WriteByte((byte)npc.LightModifier);
            }

            if (npc.ShadowModifier != 0)
            {
                writer.WriteByte(NpcTypeOpcodes.ShadowModifier);
                writer.WriteByte((byte)(npc.ShadowModifier / 5));
            }

            if (npc.HeadIcon != -1)
            {
                writer.WriteByte(NpcTypeOpcodes.HeadIcon);
                writer.WriteShort(npc.HeadIcon);
            }

            if (npc.DegreesToTurn != 32)
            {
                writer.WriteByte(NpcTypeOpcodes.DegreesToTurn);
                writer.WriteShort(npc.DegreesToTurn);
            }

            if (npc.TransformToIDs != null && npc.TransformToIDs.Length > 1)
            {
                var lastVal = npc.TransformToIDs[npc.TransformToIDs.Length - 1];
                var baseOpcode = NpcTypeOpcodes.TransformToIDs1;
                if (lastVal != -1)
                {
                    baseOpcode = NpcTypeOpcodes.TransformToIDs2;
                }

                writer.WriteByte((byte)baseOpcode);
                writer.WriteShort(npc.VarpBitFileID);
                writer.WriteShort(npc.ConfigID);

                if (baseOpcode == NpcTypeOpcodes.TransformToIDs2)
                {
                    writer.WriteShort(lastVal);
                }

                var baseModelIndex = npc.TransformToIDs.Length - 2;
                writer.WriteByte((byte)baseModelIndex);

                for (var i = 0; i <= baseModelIndex; i++)
                {
                    writer.WriteShort(npc.TransformToIDs[i]);
                }
            }

            if (!npc.IsClickable)
            {
                writer.WriteByte(NpcTypeOpcodes.IsClickable);
            }

            if (!npc.ABoolean1470)
            {
                writer.WriteByte(NpcTypeOpcodes.ABoolean1470);
            }

            if (!npc.ABoolean1457)
            {
                writer.WriteByte(NpcTypeOpcodes.ABoolean1457);
            }

            if (npc.AShort1495 != 0 || npc.AShort1490 != 0)
            {
                writer.WriteByte(NpcTypeOpcodes.AShorts);
                writer.WriteShort(npc.AShort1495);
                writer.WriteShort(npc.AShort1490);
            }

            if (npc.AByte1447 != -96 || npc.AByte1445 != -16)
            {
                writer.WriteByte(NpcTypeOpcodes.ABytes);
                writer.WriteByte((byte)npc.AByte1447);
                writer.WriteByte((byte)npc.AByte1445);
            }

            if (npc.WalkingProperties != 0)
            {
                writer.WriteByte(NpcTypeOpcodes.WalkingProperties);
                writer.WriteByte((byte)npc.WalkingProperties);
            }

            if (npc.AnIntArrayArray1449 != null && npc.ModelIDs != null)
            {
                writer.WriteByte(NpcTypeOpcodes.AnIntArrayArray1449);
                int count = 0;
                for(int i = 0; i < npc.AnIntArrayArray1449.Length; i++)
                {
                    if (npc.AnIntArrayArray1449[i] != null)
                    {
                        count++;
                    }
                }
                writer.WriteByte((byte)count);

                for (int i = 0; i < npc.AnIntArrayArray1449.Length; i++)
                {
                    if (npc.AnIntArrayArray1449[i] != null)
                    {
                        writer.WriteByte((byte)i);
                        writer.WriteByte((byte)npc.AnIntArrayArray1449[i][0]);
                        writer.WriteByte((byte)npc.AnIntArrayArray1449[i][1]);
                        writer.WriteByte((byte)npc.AnIntArrayArray1449[i][2]);
                    }
                }
            }

            if (npc.AnInt1462 != -1)
            {
                writer.WriteByte(NpcTypeOpcodes.AnInt1462);
                writer.WriteShort(npc.AnInt1462);
            }

            if (npc.SpawnFaceDirection != 7)
            {
                writer.WriteByte(NpcTypeOpcodes.SpawnFaceDirection);
                writer.WriteByte((byte)npc.SpawnFaceDirection);
            }

            if (npc.RenderId != -1)
            {
                writer.WriteByte(NpcTypeOpcodes.RenderId);
                writer.WriteShort(npc.RenderId);
            }

            if (npc.Speed != 0)
            {
                writer.WriteByte(NpcTypeOpcodes.Speed);
                writer.WriteByte((byte)npc.Speed);
            }

            if (npc.IdleAnimationId != -1 || npc.MoveType1AnimationId != -1 || npc.MoveType2AnimationId != -1 || npc.RunAnimationId != -1)
            {
                writer.WriteByte(NpcTypeOpcodes.PassiveAnimations);
                writer.WriteShort(npc.IdleAnimationId);
                writer.WriteShort(npc.MoveType1AnimationId);
                writer.WriteShort(npc.MoveType2AnimationId);
                writer.WriteShort(npc.RunAnimationId);
                writer.WriteByte(npc.AnInt1504);
            }

            if (npc.AnInt1480 != -1 || npc.AnInt1453 != -1)
            {
                writer.WriteByte(NpcTypeOpcodes.AnInts1);
                writer.WriteByte((byte)npc.AnInt1480);
                writer.WriteShort(npc.AnInt1453);
            }

            if (npc.AnInt1510 != -1 || npc.AnInt1475 != -1)
            {
                writer.WriteByte(NpcTypeOpcodes.AnInts2);
                writer.WriteByte((byte)npc.AnInt1510);
                writer.WriteShort(npc.AnInt1475);
            }

            if (npc.AttackCursor != -1)
            {
                writer.WriteByte(NpcTypeOpcodes.AttackCursor);
                writer.WriteShort(npc.AttackCursor);
            }

            if (npc.AnInt1507 != -1)
            {
                writer.WriteByte(NpcTypeOpcodes.AnInt1507);
                writer.WriteBigSmart(npc.AnInt1507);
            }

            if (npc.AnInt1456 != 255)
            {
                writer.WriteByte(NpcTypeOpcodes.AnInt1456);
                writer.WriteByte((byte)npc.AnInt1456);
            }

            if (npc.ABoolean1511)
            {
                writer.WriteByte(NpcTypeOpcodes.ABoolean1511);
            }

            if (npc.MapIcon != -1)
            {
                writer.WriteByte(NpcTypeOpcodes.MapIcon);
                writer.WriteShort(npc.MapIcon);
            }

            if (npc.ABoolean1483)
            {
                writer.WriteByte(NpcTypeOpcodes.ABoolean1483);
            }

            for (int i = 0; i < 5; i++)
            {
                if (npc.Actions[i] != null && npc.Actions[i] == "Attack")
                {
                    writer.WriteByte((byte)(NpcTypeOpcodes.ActionsAttack + i));
                    writer.WriteString(npc.Actions[i]);
                }
            }

            if (npc.ModelRedColor != 0 || npc.ModelGreenColor != 0 || npc.ModelBlueColor != 0 || npc.ModelAlphaColor != 0)
            {
                writer.WriteByte(NpcTypeOpcodes.ModelColors);
                writer.WriteByte((byte)npc.ModelRedColor);
                writer.WriteByte((byte)npc.ModelGreenColor);
                writer.WriteByte((byte)npc.ModelBlueColor);
                writer.WriteByte((byte)npc.ModelAlphaColor);
            }

            if (npc.AByte1487 == 1)
            {
                writer.WriteByte(NpcTypeOpcodes.AByte1487_1);
            }

            if (npc.AByte1487 == 0)
            {
                writer.WriteByte(NpcTypeOpcodes.AByte1487_0);
            }

            if (npc.QuestIDs != null)
            {
                writer.WriteByte(NpcTypeOpcodes.QuestIDs);
                writer.WriteByte((byte)npc.QuestIDs.Length);
                foreach (var questId in npc.QuestIDs)
                {
                    writer.WriteShort(questId);
                }
            }

            if (npc.HasDisplayName)
            {
                writer.WriteByte(NpcTypeOpcodes.HasDisplayName);
            }

            if (npc.AnInt1454 != -1)
            {
                writer.WriteByte(NpcTypeOpcodes.AnInt1454);
                writer.WriteByte((byte)npc.AnInt1454);
            }

            if (npc.AnInt1502 != 256 || npc.AnInt1463 != 256)
            {
                writer.WriteByte(NpcTypeOpcodes.AnInts3);
                writer.WriteShort(npc.AnInt1502);
                writer.WriteShort(npc.AnInt1463);
            }

            if (npc.AnInt1497 != 0)
            {
                writer.WriteByte(NpcTypeOpcodes.AnInt1497);
                writer.WriteByte((byte)npc.AnInt1497);
            }

            if (npc.AnInt1464 != 0)
            {
                writer.WriteByte(NpcTypeOpcodes.AnInt1464);
                writer.WriteByte((byte)npc.AnInt1464);
            }

            if (npc.ExtraData != null)
            {
                writer.WriteByte(NpcTypeOpcodes.ExtraData);
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


            writer.WriteByte(NpcTypeOpcodes.End);
            return writer;
        }

        private void Decode(NpcType npcType, MemoryStream stream)
        {
            while (true)
            {
                int opcode = stream.ReadUnsignedByte();
                if (opcode == NpcTypeOpcodes.End)
                {
                    return;
                }
                if (opcode != NpcTypeOpcodes.ModelIDs)
                {
                    if (opcode == NpcTypeOpcodes.Name)
                        npcType.Name = stream.ReadString();
                    else if (opcode != NpcTypeOpcodes.Size)
                    {
                        if (opcode >= NpcTypeOpcodes.Actions && opcode < 35)
                            npcType.Actions[opcode - 30] = stream.ReadString();
                        else if (opcode == NpcTypeOpcodes.ModifiedColours)
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
                        else if (opcode != NpcTypeOpcodes.AShortArrays)
                        {
                            if (opcode == NpcTypeOpcodes.AByteArray1446)
                            {
                                int i4 = stream.ReadUnsignedByte();
                                npcType.AByteArray1446 = new sbyte[i4];
                                for (int i5 = 0; i4 > i5; i5++)
                                    npcType.AByteArray1446[i5] = (sbyte)stream.ReadSignedByte();
                            }
                            else if (opcode == 44)
                            {
                                int i152 = stream.ReadUnsignedShort();
                            }
                            else if (opcode == 45)
                            {
                                int i157 = stream.ReadUnsignedShort();
                            }
                            else if (opcode == NpcTypeOpcodes.InterfaceModelIDs)
                            {
                                int i6 = stream.ReadUnsignedByte();
                                npcType.InterfaceModelIDs = new int[i6];
                                for (int i7 = 0; i6 > i7; i7++)
                                    npcType.InterfaceModelIDs[i7] = stream.ReadBigSmart();
                            }
                            else if (opcode == NpcTypeOpcodes.VisibleOnMinimap)
                                npcType.VisibleOnMinimap = false;
                            else if (opcode != NpcTypeOpcodes.CombatLevel)
                            {
                                if (opcode == NpcTypeOpcodes.ScaleX)
                                    npcType.ScaleX = stream.ReadUnsignedShort();
                                else if (opcode != NpcTypeOpcodes.ScaleY)
                                {
                                    if (opcode != NpcTypeOpcodes.IsVisible)
                                    {
                                        if (opcode != NpcTypeOpcodes.LightModifier)
                                        {
                                            if (opcode == NpcTypeOpcodes.ShadowModifier)
                                                npcType.ShadowModifier = (sbyte)stream.ReadSignedByte() * 5;
                                            else if (opcode == NpcTypeOpcodes.HeadIcon)
                                                npcType.HeadIcon = stream.ReadUnsignedShort();
                                            else if (opcode != NpcTypeOpcodes.DegreesToTurn)
                                            {
                                                if (opcode == NpcTypeOpcodes.TransformToIDs1 || opcode == NpcTypeOpcodes.TransformToIDs2)
                                                {
                                                    npcType.VarpBitFileID = stream.ReadUnsignedShort();
                                                    if (npcType.VarpBitFileID == 65535)
                                                        npcType.VarpBitFileID = -1;
                                                    npcType.ConfigID = stream.ReadUnsignedShort();
                                                    if (npcType.ConfigID == 65535)
                                                        npcType.ConfigID = -1;
                                                    int baseModelId = -1;
                                                    if (opcode == NpcTypeOpcodes.TransformToIDs2)
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
                                                else if (opcode != NpcTypeOpcodes.IsClickable)
                                                {
                                                    if (opcode == NpcTypeOpcodes.ABoolean1470)
                                                        npcType.ABoolean1470 = false;
                                                    else if (opcode == NpcTypeOpcodes.ABoolean1457)
                                                        npcType.ABoolean1457 = false;
                                                    else if (opcode != NpcTypeOpcodes.AShorts)
                                                    {
                                                        if (opcode != NpcTypeOpcodes.ABytes)
                                                        {
                                                            if (opcode == NpcTypeOpcodes.WalkingProperties)
                                                                npcType.WalkingProperties = ((sbyte)stream.ReadSignedByte());
                                                            else if (opcode == NpcTypeOpcodes.AnIntArrayArray1449)
                                                            {
                                                                int i11 = stream.ReadUnsignedByte();
                                                                if (npcType.ModelIDs == null)
                                                                {
                                                                    for (int i12 = 0; i12 < i11; i12++)
                                                                    {
                                                                        stream.ReadUnsignedByte();
                                                                        stream.ReadSignedByte();
                                                                        stream.ReadSignedByte();
                                                                        stream.ReadSignedByte();
                                                                    }
                                                                    continue;
                                                                }
                                                                npcType.AnIntArrayArray1449 = new int[npcType.ModelIDs.Length][];
                                                                for (int i12 = 0; i12 < i11; i12++)
                                                                {
                                                                    int i13 = stream.ReadUnsignedByte();
                                                                    int[] isa = (npcType.AnIntArrayArray1449[i13] = new int[3]);
                                                                    isa[0] = (sbyte)stream.ReadSignedByte();
                                                                    isa[1] = (sbyte)stream.ReadSignedByte();
                                                                    isa[2] = (sbyte)stream.ReadSignedByte();
                                                                }
                                                            }
                                                            else if (opcode != 122)
                                                            {
                                                                if (opcode != NpcTypeOpcodes.AnInt1462)
                                                                {
                                                                    if (opcode != NpcTypeOpcodes.SpawnFaceDirection)
                                                                    {
                                                                        if (opcode == NpcTypeOpcodes.RenderId)
                                                                            npcType.RenderId = (stream.ReadUnsignedShort());
                                                                        else if (opcode != NpcTypeOpcodes.Speed)
                                                                        {
                                                                            if (opcode == NpcTypeOpcodes.PassiveAnimations)
                                                                            {
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
                                                                            else if (opcode == NpcTypeOpcodes.AnInts1)
                                                                            {
                                                                                npcType.AnInt1480 = stream.ReadUnsignedByte();
                                                                                npcType.AnInt1453 = stream.ReadUnsignedShort();
                                                                            }
                                                                            else if (opcode == NpcTypeOpcodes.AnInts2)
                                                                            {
                                                                                npcType.AnInt1510 = stream.ReadUnsignedByte();
                                                                                npcType.AnInt1475 = stream.ReadUnsignedShort();
                                                                            }
                                                                            else if (opcode != NpcTypeOpcodes.AttackCursor)
                                                                            {
                                                                                if (opcode != NpcTypeOpcodes.AnInt1507)
                                                                                {
                                                                                    if (opcode == NpcTypeOpcodes.AnInt1456)
                                                                                        npcType.AnInt1456 = stream.ReadUnsignedByte();
                                                                                    else if (opcode != NpcTypeOpcodes.ABoolean1511)
                                                                                    {
                                                                                        if (opcode != NpcTypeOpcodes.MapIcon)
                                                                                        {
                                                                                            if (opcode == NpcTypeOpcodes.ABoolean1483)
                                                                                                npcType.ABoolean1483 = true;
                                                                                            else if (opcode >= NpcTypeOpcodes.ActionsAttack && opcode < 155)
                                                                                            {
                                                                                                npcType.Actions[opcode - 150] = stream.ReadString();
                                                                                            }
                                                                                            else if (opcode == NpcTypeOpcodes.ModelColors)
                                                                                            {
                                                                                                npcType.ModelRedColor = (sbyte)stream.ReadSignedByte();
                                                                                                npcType.ModelGreenColor = (sbyte)stream.ReadSignedByte();
                                                                                                npcType.ModelBlueColor = (sbyte)stream.ReadSignedByte();
                                                                                                npcType.ModelAlphaColor = (sbyte)stream.ReadSignedByte();
                                                                                            }
                                                                                            else if (opcode == NpcTypeOpcodes.AByte1487_1)
                                                                                                npcType.AByte1487 = 1;
                                                                                            else if (opcode != NpcTypeOpcodes.AByte1487_0)
                                                                                            {
                                                                                                if (opcode != NpcTypeOpcodes.QuestIDs)
                                                                                                {
                                                                                                    if (opcode == NpcTypeOpcodes.HasDisplayName)
                                                                                                        npcType.HasDisplayName = true;
                                                                                                    else if (opcode != NpcTypeOpcodes.AnInt1454)
                                                                                                    {
                                                                                                        if (opcode == NpcTypeOpcodes.AnInts3)
                                                                                                        {
                                                                                                            npcType.AnInt1502 = stream.ReadUnsignedShort();
                                                                                                            npcType.AnInt1463 = stream.ReadUnsignedShort();
                                                                                                        }
                                                                                                        else if (opcode == NpcTypeOpcodes.AnInt1497)
                                                                                                            npcType.AnInt1497 = stream.ReadUnsignedByte();
                                                                                                        else if (opcode != NpcTypeOpcodes.AnInt1464)
                                                                                                        {
                                                                                                            if (opcode == 169)
                                                                                                            {
                                                                                                            }
                                                                                                            else if (opcode >= 170 && opcode < 176)
                                                                                                            {
                                                                                                                int unknownShort = stream.ReadUnsignedShort();
                                                                                                                if (unknownShort == 65535)
                                                                                                                    unknownShort = -1;
                                                                                                            }
                                                                                                            else if (opcode == NpcTypeOpcodes.ExtraData)
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
                                                                npcType.AnInt1485 = 0;
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