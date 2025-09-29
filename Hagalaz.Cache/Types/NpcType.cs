using System;
using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// Represents NPC type.
    /// </summary>
    public class NpcType : IType, INpcType
    {
        /// <summary>
        /// Contains item extra data.
        /// </summary>
        /// <value>The extra data.</value>
        public Dictionary<int, object>? ExtraData { get; private set; }
        /// <summary>
        /// Contains Id of this npc.
        /// </summary>
        /// <value>The Id.</value>
        public int Id { get; }
        /// <summary>
        /// Contains name of this npc.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets the actions.
        /// </summary>
        /// <value>The actions.</value>
        public string?[] Actions { get; private set; }
        /// <summary>
        /// Gets a byte1445.
        /// </summary>
        /// <value>A byte1445.</value>
        public sbyte AByte1445 { get; private set; }
        /// <summary>
        /// Gets a byte array1446.
        /// </summary>
        /// <value>A byte array1446.</value>
        public sbyte[] AByteArray1446 { get; private set; }
        /// <summary>
        /// Gets a byte1447.
        /// </summary>
        /// <value>A byte1447.</value>
        public sbyte AByte1447 { get; private set; }
        /// <summary>
        /// Gets an int1448.
        /// </summary>
        /// <value>An int1448.</value>
        public int MapIcon { get; private set; }
        /// <summary>
        /// Gets an int array array1449.
        /// </summary>
        /// <value>An int array array1449.</value>
        public int[][] AnIntArrayArray1449 { get; private set; }
        /// <summary>
        /// Gets an int array1451.
        /// </summary>
        /// <value>An int array1451.</value>
        public int[]? TransformToIDs { get; private set; }
        /// <summary>
        /// Gets an int1453.
        /// </summary>
        /// <value>An int1453.</value>
        public int AnInt1453 { get; private set; }
        /// <summary>
        /// Gets an int1454.
        /// </summary>
        /// <value>An int1454.</value>
        public int AnInt1454 { get; private set; }
        /// <summary>
        /// Gets an int1456.
        /// </summary>
        /// <value>An int1456.</value>
        public int AnInt1456 { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean1457].
        /// </summary>
        /// <value><c>true</c> if [a boolean1457]; otherwise, <c>false</c>.</value>
        public bool ABoolean1457 { get; private set; }
        /// <summary>
        /// Gets a value indicating whether this instance is clickable.
        /// </summary>
        /// <value><c>true</c> if this instance is clickable; otherwise, <c>false</c>.</value>
        public bool IsClickable { get; private set; }
        /// <summary>
        /// Gets an int1459.
        /// </summary>
        /// <value>An int1459.</value>
        public int DegreesToTurn { get; private set; }
        /// <summary>
        /// Gets an int1460.
        /// </summary>
        /// <value>An int1460.</value>
        public int VarpBitFileID { get; private set; }
        /// <summary>
        /// Gets an int1461.
        /// </summary>
        /// <value>An int1461.</value>
        public int ConfigID { get; private set; }
        /// <summary>
        /// Gets an int1462.
        /// </summary>
        /// <value>An int1462.</value>
        public int AnInt1462 { get; private set; }
        /// <summary>
        /// Gets an int1463.
        /// </summary>
        /// <value>An int1463.</value>
        public int AnInt1463 { get; private set; }
        /// <summary>
        /// Gets an int1464.
        /// </summary>
        /// <value>An int1464.</value>
        public int AnInt1464 { get; private set; }
        /// <summary>
        /// Gets an int1465.
        /// </summary>
        /// <value>An int1465.</value>
        public int AnInt1465 { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [visible on minimap].
        /// </summary>
        /// <value><c>true</c> if [visible on minimap]; otherwise, <c>false</c>.</value>
        public bool VisibleOnMinimap { get; private set; }
        /// <summary>
        /// Gets an int1468.
        /// </summary>
        /// <value>An int1468.</value>
        public int ScaleY { get; private set; }
        /// <summary>
        /// Gets the spawn direction.
        /// </summary>
        /// <value>The spawn direction.</value>
        public sbyte SpawnFaceDirection { get; set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean1470].
        /// </summary>
        /// <value><c>true</c> if [a boolean1470]; otherwise, <c>false</c>.</value>
        public bool ABoolean1470 { get; private set; }
        /// <summary>
        /// Gets a byte1471.
        /// </summary>
        /// <value>A byte1471.</value>
        public sbyte ModelGreenColor { get; private set; }
        /// <summary>
        /// Gets an int1472.
        /// </summary>
        /// <value>An int1472.</value>
        public int LightModifier { get; private set; }

        /// <summary>
        /// Gets the render emote.
        /// </summary>
        /// <value>The render emote.</value>
        public int RenderId { get; private set; } = 1426;
        /// <summary>
        /// Gets a byte1474.
        /// </summary>
        /// <value>A byte1474.</value>
        public sbyte ModelBlueColor { get; private set; }
        /// <summary>
        /// Gets an int1475.
        /// </summary>
        /// <value>An int1475.</value>
        public int AnInt1475 { get; private set; }
        /// <summary>
        /// Gets a byte1476.
        /// </summary>
        /// <value>A byte1476.</value>
        public sbyte ModelRedColor { get; private set; }
        /// <summary>
        /// Gets the combat level.
        /// </summary>
        /// <value>The combat level.</value>
        public int CombatLevel { get; set; }
        /// <summary>
        /// Gets an int1479.
        /// </summary>
        /// <value>An int1479.</value>
        public int ShadowModifier { get; private set; }
        /// <summary>
        /// Gets an int1480.
        /// </summary>
        /// <value>An int1480.</value>
        public int AnInt1480 { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean1481].
        /// </summary>
        /// <value><c>true</c> if [a boolean1481]; otherwise, <c>false</c>.</value>
        public bool HasDisplayName { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean1483].
        /// </summary>
        /// <value><c>true</c> if [a boolean1483]; otherwise, <c>false</c>.</value>
        public bool ABoolean1483 { get; private set; }
        /// <summary>
        /// Gets an int1484.
        /// </summary>
        /// <value>An int1484.</value>
        public int RunAnimationId { get; private set; }
        /// <summary>
        /// Gets an int1485.
        /// </summary>
        /// <value>An int1485.</value>
        public int AnInt1485 { get; private set; }
        /// <summary>
        /// Gets an int1486.
        /// </summary>
        /// <value>An int1486.</value>
        public int IdleAnimationId { get; private set; }
        /// <summary>
        /// Gets a byte1487.
        /// </summary>
        /// <value>A byte1487.</value>
        public sbyte AByte1487 { get; private set; }
        /// <summary>
        /// Gets the size.
        /// </summary>
        /// <value>The size.</value>
        public int Size { get; set; }
        /// <summary>
        /// Gets the original colours.
        /// </summary>
        /// <value>The original colours.</value>
        public short[]? OriginalColours { get; private set; }
        /// <summary>
        /// Gets a short1490.
        /// </summary>
        /// <value>A short1490.</value>
        public short AShort1490 { get; private set; }
        /// <summary>
        /// Gets a short array1491.
        /// </summary>
        /// <value>A short array1491.</value>
        public short[]? AShortArray1491 { get; private set; }
        /// <summary>
        /// Gets an int1492.
        /// </summary>
        /// <value>An int1492.</value>
        public int AttackCursor { get; private set; }
        /// <summary>
        /// Gets the walking properties.
        /// </summary>
        /// <value>The walking properties.</value>
        public sbyte WalkingProperties { get; private set; }
        /// <summary>
        /// Gets a byte1494.
        /// </summary>
        /// <value>A byte1494.</value>
        public sbyte ModelAlphaColor { get; private set; }
        /// <summary>
        /// Gets a short1495.
        /// </summary>
        /// <value>A short1495.</value>
        public short AShort1495 { get; private set; }
        /// <summary>
        /// Gets an int array1496.
        /// </summary>
        /// <value>An int array1496.</value>
        public int[]? InterfaceModelIDs { get; private set; }
        /// <summary>
        /// Gets an int1497.
        /// </summary>
        /// <value>An int1497.</value>
        public int AnInt1497 { get; private set; }
        /// <summary>
        /// Gets an int1498.
        /// </summary>
        /// <value>An int1498.</value>
        public int ScaleX { get; private set; }
        /// <summary>
        /// Gets an int array1499.
        /// </summary>
        /// <value>An int array1499.</value>
        public int[]? QuestIDs { get; private set; }
        /// <summary>
        /// Gets an int1500.
        /// </summary>
        /// <value>An int1500.</value>
        public int MoveType2AnimationId { get; private set; }
        /// <summary>
        /// Gets an int1501.
        /// </summary>
        /// <value>An int1501.</value>
        public int MoveType1AnimationId { get; private set; }
        /// <summary>
        /// Gets an int1502.
        /// </summary>
        /// <value>An int1502.</value>
        public int AnInt1502 { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean1503].
        /// </summary>
        /// <value><c>true</c> if [a boolean1503]; otherwise, <c>false</c>.</value>
        public bool IsVisible { get; private set; }
        /// <summary>
        /// Gets an int1504.
        /// </summary>
        /// <value>An int1504.</value>
        public int AnInt1504 { get; private set; }
        /// <summary>
        /// Gets the modified colours.
        /// </summary>
        /// <value>The modified colours.</value>
        public short[]? ModifiedColours { get; private set; }
        /// <summary>
        /// Gets the head icon.
        /// </summary>
        /// <value>The head icon.</value>
        public int HeadIcon { get; private set; }
        /// <summary>
        /// Gets an int1507.
        /// </summary>
        /// <value>An int1507.</value>
        public int AnInt1507 { get; private set; }
        /// <summary>
        /// Gets a short array1508.
        /// </summary>
        /// <value>A short array1508.</value>
        public short[]? AShortArray1508 { get; private set; }
        /// <summary>
        /// Gets an int array1509.
        /// </summary>
        /// <value>An int array1509.</value>
        public int[]? ModelIDs { get; private set; }
        /// <summary>
        /// Gets an int1510.
        /// </summary>
        /// <value>An int1510.</value>
        public int AnInt1510 { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean1511].
        /// </summary>
        /// <value><c>true</c> if [a boolean1511]; otherwise, <c>false</c>.</value>
        public bool ABoolean1511 { get; private set; }
        /// <summary>
        /// Gets an opc128.
        /// </summary>
        /// <value>An opc128.</value>
        public int Speed { get; private set; }

        /// <summary>
        /// Construct's new NPCDefinition instance.
        /// </summary>
        /// <param name="id">Id of the npc.</param>
        public NpcType(int id)
        {
            Id = id;
            Actions = [null, null, null, null, null, "Examine"];
            Name = "null";
            AByte1445 = -16;
            ConfigID = -1;
            IsClickable = true;
            AnInt1465 = -1;
            AnInt1453 = -1;
            DegreesToTurn = 32;
            VarpBitFileID = -1;
            AnInt1456 = 255;
            AnInt1463 = 256;
            ABoolean1470 = true;
            AnInt1462 = -1;
            AByte1447 = -96;
            ABoolean1483 = false;
            ScaleY = 128;
            AnInt1480 = -1;
            MapIcon = -1;
            RenderId = -1;
            LightModifier = 0;
            SpawnFaceDirection = 4; // south
            AByte1487 = -1;
            Size = 1;
            AnInt1454 = -1;
            AnInt1485 = -1;
            IdleAnimationId = -1;
            ABoolean1457 = true;
            WalkingProperties = 0;
            AShort1490 = 0;
            AttackCursor = -1;
            AnInt1464 = 0;
            AnInt1504 = 0;
            ScaleX = 128;
            AnInt1475 = -1;
            RunAnimationId = -1;
            HeadIcon = -1;
            AnInt1497 = 0;
            VisibleOnMinimap = true;
            IsVisible = false;
            AnInt1502 = 256;
            MoveType2AnimationId = -1;
            ModelAlphaColor = 0;
            ShadowModifier = 0;
            AnInt1507 = -1;
            MoveType1AnimationId = -1;
            AShort1495 = 0;
            CombatLevel = -1;
            ABoolean1511 = false;
            AnInt1510 = -1;
            ExtraData = null;
            Speed = 0;
            AByteArray1446 = Array.Empty<sbyte>();
            AnIntArrayArray1449 = Array.Empty<int[]>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        public void Decode(MemoryStream stream)
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
                        Name = stream.ReadString();
                    else if (opcode != 12)
                    {
                        if (opcode >= 30 && opcode < 35)
                            Actions[opcode - 30] = stream.ReadString();
                        else if (opcode == 40)
                        {
                            int len = stream.ReadUnsignedByte();
                            ModifiedColours = new short[len];
                            OriginalColours = new short[len];
                            for (int i = 0; len > i; i++)
                            {
                                OriginalColours[i] = (short)stream.ReadUnsignedShort();
                                ModifiedColours[i] = (short)stream.ReadUnsignedShort();
                            }
                        }
                        else if (opcode != 41)
                        {
                            if (opcode == 42)
                            {
                                int i4 = stream.ReadUnsignedByte();
                                AByteArray1446 = new sbyte[i4];
                                for (int i5 = 0; i4 > i5; i5++)
                                    AByteArray1446[i5] = (sbyte)stream.ReadSignedByte();
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
                                InterfaceModelIDs = new int[i6];
                                for (int i7 = 0; i6 > i7; i7++)
                                    InterfaceModelIDs[i7] = stream.ReadBigSmart();
                            }
                            else if (opcode == 93)
                                VisibleOnMinimap = false;
                            else if (opcode != 95)
                            {
                                if (opcode == 97)
                                    ScaleX = stream.ReadUnsignedShort();
                                else if (opcode != 98)
                                {
                                    if (opcode != 99)
                                    {
                                        if (opcode != 100)
                                        {
                                            if (opcode == 101)
                                                ShadowModifier = (sbyte)stream.ReadSignedByte() * 5;
                                            else if (opcode == 102)
                                                HeadIcon = stream.ReadUnsignedShort();
                                            else if (opcode != 103)
                                            {
                                                if (opcode == 106 || opcode == 118)
                                                {
                                                    VarpBitFileID = stream.ReadUnsignedShort();
                                                    if (VarpBitFileID == 65535)
                                                        VarpBitFileID = -1;
                                                    ConfigID = stream.ReadUnsignedShort();
                                                    if (ConfigID == 65535)
                                                        ConfigID = -1;
                                                    int baseModelId = -1;
                                                    if (opcode == 118)
                                                    {
                                                        baseModelId = stream.ReadUnsignedShort();
                                                        if (baseModelId == 65535)
                                                            baseModelId = -1;
                                                    }

                                                    int baseModelIndex = stream.ReadUnsignedByte();
                                                    TransformToIDs = new int[baseModelIndex + 2];
                                                    for (int i10 = 0; baseModelIndex >= i10; i10++)
                                                    {
                                                        TransformToIDs[i10] = (stream.ReadUnsignedShort());
                                                        if (TransformToIDs[i10] == 65535)
                                                            TransformToIDs[i10] = -1;
                                                    }

                                                    TransformToIDs[baseModelIndex + 1] = baseModelId;
                                                }
                                                else if (opcode != 107)
                                                {
                                                    if (opcode == 109)
                                                        ABoolean1470 = false;
                                                    else if (opcode == 111)
                                                        ABoolean1457 = false;
                                                    else if (opcode != 113)
                                                    {
                                                        if (opcode != 114)
                                                        {
                                                            if (opcode == 119)
                                                                WalkingProperties = ((sbyte)stream.ReadSignedByte());
                                                            else if (opcode == 121)
                                                            {
                                                                AnIntArrayArray1449 = (new int[ModelIDs.Length][]);
                                                                int i11 = (stream.ReadUnsignedByte());
                                                                for (int i12 = 0; i12 < i11; i12++)
                                                                {
                                                                    int i13 = (stream.ReadUnsignedByte());
                                                                    int[] isa = (AnIntArrayArray1449[i13] = (new int[3]));
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
                                                                            RenderId = (stream.ReadUnsignedShort());
                                                                        else if (opcode != 128)
                                                                        {
                                                                            if (opcode == 134)
                                                                            {
                                                                                // passive anims
                                                                                IdleAnimationId = stream.ReadUnsignedShort();
                                                                                if (IdleAnimationId == 65535)
                                                                                    IdleAnimationId = -1;
                                                                                MoveType1AnimationId = stream.ReadUnsignedShort();
                                                                                if (MoveType1AnimationId == 65535)
                                                                                    MoveType1AnimationId = -1;
                                                                                MoveType2AnimationId = stream.ReadUnsignedShort();
                                                                                if (MoveType2AnimationId == 65535)
                                                                                    MoveType2AnimationId = -1;
                                                                                RunAnimationId = stream.ReadUnsignedShort();
                                                                                if (RunAnimationId == 65535)
                                                                                    RunAnimationId = -1;
                                                                                AnInt1504 = stream.ReadUnsignedByte();
                                                                            }
                                                                            else if (opcode == 135)
                                                                            {
                                                                                AnInt1480 = stream.ReadUnsignedByte();
                                                                                AnInt1453 = stream.ReadUnsignedShort();
                                                                            }
                                                                            else if (opcode == 136)
                                                                            {
                                                                                AnInt1510 = stream.ReadUnsignedByte();
                                                                                AnInt1475 = stream.ReadUnsignedShort();
                                                                            }
                                                                            else if (opcode != 137)
                                                                            {
                                                                                if (opcode != 138)
                                                                                {
                                                                                    if (opcode == 140)
                                                                                        AnInt1456 = stream.ReadUnsignedByte(); // passive anim
                                                                                    else if (opcode != 141)
                                                                                    {
                                                                                        if (opcode != 142)
                                                                                        {
                                                                                            if (opcode == 143)
                                                                                                ABoolean1483 = true;
                                                                                            else if (opcode >= 150 && opcode < 155)
                                                                                            {
                                                                                                Actions[opcode - 150] = stream.ReadString();
                                                                                            }
                                                                                            else if (opcode == 155)
                                                                                            {
                                                                                                ModelRedColor = (sbyte)stream.ReadSignedByte();
                                                                                                ModelGreenColor = (sbyte)stream.ReadSignedByte();
                                                                                                ModelBlueColor = (sbyte)stream.ReadSignedByte();
                                                                                                ModelAlphaColor = (sbyte)stream.ReadSignedByte();
                                                                                            }
                                                                                            else if (opcode == 158)
                                                                                                AByte1487 = 1;
                                                                                            else if (opcode != 159)
                                                                                            {
                                                                                                if (opcode != 160)
                                                                                                {
                                                                                                    if (opcode == 162)
                                                                                                        HasDisplayName = true;
                                                                                                    else if (opcode != 163)
                                                                                                    {
                                                                                                        if (opcode == 164)
                                                                                                        {
                                                                                                            AnInt1502 = stream.ReadUnsignedShort();
                                                                                                            AnInt1463 = stream.ReadUnsignedShort();
                                                                                                        }
                                                                                                        else if (opcode == 165)
                                                                                                            AnInt1497 = stream.ReadUnsignedByte();
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
                                                                                                                ExtraData = new Dictionary<int, object>(i14);
                                                                                                                for (int i16 = 0; i14 > i16; i16++)
                                                                                                                {
                                                                                                                    bool stringValue = stream.ReadUnsignedByte() == 1;
                                                                                                                    int key = stream.ReadMedInt();

                                                                                                                    if (ExtraData.ContainsKey(key))
                                                                                                                        ExtraData.Remove(key);

                                                                                                                    if (stringValue)
                                                                                                                        ExtraData.Add(key, stream.ReadString());
                                                                                                                    else
                                                                                                                        ExtraData.Add(key, stream.ReadInt());
                                                                                                                }
                                                                                                            }
                                                                                                        }
                                                                                                        else
                                                                                                            AnInt1464 = stream.ReadUnsignedByte();
                                                                                                    }
                                                                                                    else
                                                                                                        AnInt1454 = stream.ReadUnsignedByte();
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    int i18 = stream.ReadUnsignedByte();
                                                                                                    QuestIDs = new int[i18];
                                                                                                    for (int i19 = 0; i19 < i18; i19++)
                                                                                                        QuestIDs[i19] = stream.ReadUnsignedShort();
                                                                                                }
                                                                                            }
                                                                                            else
                                                                                                AByte1487 = 0;
                                                                                        }
                                                                                        else
                                                                                            MapIcon = stream.ReadUnsignedShort();
                                                                                    }
                                                                                    else
                                                                                        ABoolean1511 = true;
                                                                                }
                                                                                else
                                                                                    AnInt1507 = stream.ReadBigSmart();
                                                                            }
                                                                            else
                                                                                AttackCursor = stream.ReadUnsignedShort();
                                                                        }
                                                                        else
                                                                            Speed = stream.ReadUnsignedByte();
                                                                    }
                                                                    else
                                                                        SpawnFaceDirection = ((sbyte)stream.ReadSignedByte());
                                                                }
                                                                else
                                                                    AnInt1462 = (stream.ReadUnsignedShort());
                                                            }
                                                            else
                                                                AnInt1485 = 0; // not used
                                                        }
                                                        else
                                                        {
                                                            AByte1447 = (sbyte)stream.ReadSignedByte();
                                                            AByte1445 = (sbyte)stream.ReadSignedByte();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AShort1495 = (short)(stream.ReadUnsignedShort());
                                                        AShort1490 = (short)(stream.ReadUnsignedShort());
                                                    }
                                                }
                                                else
                                                    IsClickable = false;
                                            }
                                            else
                                                DegreesToTurn = stream.ReadUnsignedShort();
                                        }
                                        else
                                            LightModifier = stream.ReadSignedByte();
                                    }
                                    else
                                        IsVisible = true;
                                }
                                else
                                    ScaleY = stream.ReadUnsignedShort();
                            }
                            else
                                CombatLevel = stream.ReadUnsignedShort();
                        }
                        else
                        {
                            int i20 = stream.ReadUnsignedByte();
                            AShortArray1508 = new short[i20];
                            AShortArray1491 = new short[i20];
                            for (int i21 = 0; i20 > i21; i21++)
                            {
                                AShortArray1508[i21] = (short)stream.ReadUnsignedShort();
                                AShortArray1491[i21] = (short)stream.ReadUnsignedShort();
                            }
                        }
                    }
                    else
                        Size = stream.ReadUnsignedByte();
                }
                else
                {
                    int modelCount = stream.ReadUnsignedByte();
                    ModelIDs = new int[modelCount];
                    for (int index = 0; modelCount > index; index++)
                    {
                        ModelIDs[index] = stream.ReadBigSmart();
                        if (ModelIDs[index] == 65535)
                            ModelIDs[index] = -1;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public MemoryStream Encode() => throw new NotImplementedException();
    }
}
