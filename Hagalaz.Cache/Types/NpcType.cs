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
    public class NpcType : INpcType
    {
        /// <summary>
        /// Contains item extra data.
        /// </summary>
        /// <value>The extra data.</value>
        public Dictionary<int, object>? ExtraData { get; internal set; }
        /// <summary>
        /// Contains Id of this npc.
        /// </summary>
        /// <value>The Id.</value>
        public int Id { get; }
        /// <summary>
        /// Contains name of this npc.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; internal set; }
        /// <summary>
        /// Gets the actions.
        /// </summary>
        /// <value>The actions.</value>
        public string?[] Actions { get; internal set; }
        /// <summary>
        /// Gets a byte1445.
        /// </summary>
        /// <value>A byte1445.</value>
        public sbyte AByte1445 { get; internal set; }
        /// <summary>
        /// Gets a byte array1446.
        /// </summary>
        /// <value>A byte array1446.</value>
        public sbyte[] AByteArray1446 { get; internal set; }
        /// <summary>
        /// Gets a byte1447.
        /// </summary>
        /// <value>A byte1447.</value>
        public sbyte AByte1447 { get; internal set; }
        /// <summary>
        /// Gets an int1448.
        /// </summary>
        /// <value>An int1448.</value>
        public int MapIcon { get; internal set; }
        /// <summary>
        /// Gets an int array array1449.
        /// </summary>
        /// <value>An int array array1449.</value>
        public int[][] AnIntArrayArray1449 { get; internal set; }
        /// <summary>
        /// Gets an int array1451.
        /// </summary>
        /// <value>An int array1451.</value>
        public int[]? TransformToIDs { get; internal set; }
        /// <summary>
        /// Gets an int1453.
        /// </summary>
        /// <value>An int1453.</value>
        public int AnInt1453 { get; internal set; }
        /// <summary>
        /// Gets an int1454.
        /// </summary>
        /// <value>An int1454.</value>
        public int AnInt1454 { get; internal set; }
        /// <summary>
        /// Gets an int1456.
        /// </summary>
        /// <value>An int1456.</value>
        public int AnInt1456 { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean1457].
        /// </summary>
        /// <value><c>true</c> if [a boolean1457]; otherwise, <c>false</c>.</value>
        public bool ABoolean1457 { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether this instance is clickable.
        /// </summary>
        /// <value><c>true</c> if this instance is clickable; otherwise, <c>false</c>.</value>
        public bool IsClickable { get; internal set; }
        /// <summary>
        /// Gets an int1459.
        /// </summary>
        /// <value>An int1459.</value>
        public int DegreesToTurn { get; internal set; }
        /// <summary>
        /// Gets an int1460.
        /// </summary>
        /// <value>An int1460.</value>
        public int VarpBitFileID { get; internal set; }
        /// <summary>
        /// Gets an int1461.
        /// </summary>
        /// <value>An int1461.</value>
        public int ConfigID { get; internal set; }
        /// <summary>
        /// Gets an int1462.
        /// </summary>
        /// <value>An int1462.</value>
        public int AnInt1462 { get; internal set; }
        /// <summary>
        /// Gets an int1463.
        /// </summary>
        /// <value>An int1463.</value>
        public int AnInt1463 { get; internal set; }
        /// <summary>
        /// Gets an int1464.
        /// </summary>
        /// <value>An int1464.</value>
        public int AnInt1464 { get; internal set; }
        /// <summary>
        /// Gets an int1465.
        /// </summary>
        /// <value>An int1465.</value>
        public int AnInt1465 { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [visible on minimap].
        /// </summary>
        /// <value><c>true</c> if [visible on minimap]; otherwise, <c>false</c>.</value>
        public bool VisibleOnMinimap { get; internal set; }
        /// <summary>
        /// Gets an int1468.
        /// </summary>
        /// <value>An int1468.</value>
        public int ScaleY { get; internal set; }
        /// <summary>
        /// Gets the spawn direction.
        /// </summary>
        /// <value>The spawn direction.</value>
        public sbyte SpawnFaceDirection { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean1470].
        /// </summary>
        /// <value><c>true</c> if [a boolean1470]; otherwise, <c>false</c>.</value>
        public bool ABoolean1470 { get; internal set; }
        /// <summary>
        /// Gets a byte1471.
        /// </summary>
        /// <value>A byte1471.</value>
        public sbyte ModelGreenColor { get; internal set; }
        /// <summary>
        /// Gets an int1472.
        /// </summary>
        /// <value>An int1472.</value>
        public int LightModifier { get; internal set; }

        /// <summary>
        /// Gets the render emote.
        /// </summary>
        /// <value>The render emote.</value>
        public int RenderId { get; internal set; } = 1426;
        /// <summary>
        /// Gets a byte1474.
        /// </summary>
        /// <value>A byte1474.</value>
        public sbyte ModelBlueColor { get; internal set; }
        /// <summary>
        /// Gets an int1475.
        /// </summary>
        /// <value>An int1475.</value>
        public int AnInt1475 { get; internal set; }
        /// <summary>
        /// Gets a byte1476.
        /// </summary>
        /// <value>A byte1476.</value>
        public sbyte ModelRedColor { get; internal set; }
        /// <summary>
        /// Gets the combat level.
        /// </summary>
        /// <value>The combat level.</value>
        public int CombatLevel { get; set; }
        /// <summary>
        /// Gets an int1479.
        /// </summary>
        /// <value>An int1479.</value>
        public int ShadowModifier { get; internal set; }
        /// <summary>
        /// Gets an int1480.
        /// </summary>
        /// <value>An int1480.</value>
        public int AnInt1480 { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean1481].
        /// </summary>
        /// <value><c>true</c> if [a boolean1481]; otherwise, <c>false</c>.</value>
        public bool HasDisplayName { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean1483].
        /// </summary>
        /// <value><c>true</c> if [a boolean1483]; otherwise, <c>false</c>.</value>
        public bool ABoolean1483 { get; internal set; }
        /// <summary>
        /// Gets an int1484.
        /// </summary>
        /// <value>An int1484.</value>
        public int RunAnimationId { get; internal set; }
        /// <summary>
        /// Gets an int1485.
        /// </summary>
        /// <value>An int1485.</value>
        public int AnInt1485 { get; internal set; }
        /// <summary>
        /// Gets an int1486.
        /// </summary>
        /// <value>An int1486.</value>
        public int IdleAnimationId { get; internal set; }
        /// <summary>
        /// Gets a byte1487.
        /// </summary>
        /// <value>A byte1487.</value>
        public sbyte AByte1487 { get; internal set; }
        /// <summary>
        /// Gets the size.
        /// </summary>
        /// <value>The size.</value>
        public int Size { get; internal set; }
        /// <summary>
        /// Gets the original colours.
        /// </summary>
        /// <value>The original colours.</value>
        public short[]? OriginalColours { get; internal set; }
        /// <summary>
        /// Gets a short1490.
        /// </summary>
        /// <value>A short1490.</value>
        public short AShort1490 { get; internal set; }
        /// <summary>
        /// Gets a short array1491.
        /// </summary>
        /// <value>A short array1491.</value>
        public short[]? AShortArray1491 { get; internal set; }
        /// <summary>
        /// Gets an int1492.
        /// </summary>
        /// <value>An int1492.</value>
        public int AttackCursor { get; internal set; }
        /// <summary>
        /// Gets the walking properties.
        /// </summary>
        /// <value>The walking properties.</value>
        public sbyte WalkingProperties { get; internal set; }
        /// <summary>
        /// Gets a byte1494.
        /// </summary>
        /// <value>A byte1494.</value>
        public sbyte ModelAlphaColor { get; internal set; }
        /// <summary>
        /// Gets a short1495.
        /// </summary>
        /// <value>A short1495.</value>
        public short AShort1495 { get; internal set; }
        /// <summary>
        /// Gets an int array1496.
        /// </summary>
        /// <value>An int array1496.</value>
        public int[]? InterfaceModelIDs { get; internal set; }
        /// <summary>
        /// Gets an int1497.
        /// </summary>
        /// <value>An int1497.</value>
        public int AnInt1497 { get; internal set; }
        /// <summary>
        /// Gets an int1498.
        /// </summary>
        /// <value>An int1498.</value>
        public int ScaleX { get; internal set; }
        /// <summary>
        /// Gets an int array1499.
        /// </summary>
        /// <value>An int array1499.</value>
        public int[]? QuestIDs { get; internal set; }
        /// <summary>
        /// Gets an int1500.
        /// </summary>
        /// <value>An int1500.</value>
        public int MoveType2AnimationId { get; internal set; }
        /// <summary>
        /// Gets an int1501.
        /// </summary>
        /// <value>An int1501.</value>
        public int MoveType1AnimationId { get; internal set; }
        /// <summary>
        /// Gets an int1502.
        /// </summary>
        /// <value>An int1502.</value>
        public int AnInt1502 { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean1503].
        /// </summary>
        /// <value><c>true</c> if [a boolean1503]; otherwise, <c>false</c>.</value>
        public bool IsVisible { get; internal set; }
        /// <summary>
        /// Gets an int1504.
        /// </summary>
        /// <value>An int1504.</value>
        public int AnInt1504 { get; internal set; }
        /// <summary>
        /// Gets the modified colours.
        /// </summary>
        /// <value>The modified colours.</value>
        public short[]? ModifiedColours { get; internal set; }
        /// <summary>
        /// Gets the head icon.
        /// </summary>
        /// <value>The head icon.</value>
        public int HeadIcon { get; internal set; }
        /// <summary>
        /// Gets an int1507.
        /// </summary>
        /// <value>An int1507.</value>
        public int AnInt1507 { get; internal set; }
        /// <summary>
        /// Gets a short array1508.
        /// </summary>
        /// <value>A short array1508.</value>
        public short[]? AShortArray1508 { get; internal set; }
        /// <summary>
        /// Gets an int array1509.
        /// </summary>
        /// <value>An int array1509.</value>
        public int[]? ModelIDs { get; internal set; }
        /// <summary>
        /// Gets an int1510.
        /// </summary>
        /// <value>An int1510.</value>
        public int AnInt1510 { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean1511].
        /// </summary>
        /// <value><c>true</c> if [a boolean1511]; otherwise, <c>false</c>.</value>
        public bool ABoolean1511 { get; internal set; }
        /// <summary>
        /// Gets an opc128.
        /// </summary>
        /// <value>An opc128.</value>
        public int Speed { get; internal set; }

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
    }
}
