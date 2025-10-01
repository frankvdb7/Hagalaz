using System;
using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// Represend's owner object definition.
    /// </summary>
    public class ObjectType : IObjectType
    {
        /// <summary>
        /// Contains object extra data.
        /// </summary>
        /// <value>The extra data.</value>
        public Dictionary<int, object>? ExtraData { get; private set; }
        /// <summary>
        /// Contains ID of this object.
        /// </summary>
        /// <value>The ID.</value>
        public int Id { get; }
        /// <summary>
        /// Contains name of this object.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets an int759.
        /// </summary>
        /// <value>An int759.</value>
        public int MapIcon { get; private set; }
        /// <summary>
        /// Gets an int760.
        /// </summary>
        /// <value>An int760.</value>
        public int OffsetY { get; private set; }
        /// <summary>
        /// Gets an int761.
        /// </summary>
        /// <value>An int761.</value>
        public int AmbientSoundHearDistance { get; private set; }
        /// <summary>
        /// Gets a short array762.
        /// </summary>
        /// <value>A short array762.</value>
        public short[]? AShortArray762 { get; private set; }
        /// <summary>
        /// Gets an int763.
        /// </summary>
        /// <value>An int763.</value>
        public int MapSpriteRotation { get; private set; }
        /// <summary>
        /// Gets an int764.
        /// </summary>
        /// <value>An int764.</value>
        public int AnInt764 { get; private set; }
        /// <summary>
        /// Gets an int765.
        /// </summary>
        /// <value>An int765.</value>
        public int MapSpriteType { get; private set; }
        /// <summary>
        /// Gets an int array766.
        /// </summary>
        /// <value>An int array766.</value>
        public int[]? AudioTracks { get; private set; }
        /// <summary>
        /// Gets an int768.
        /// </summary>
        /// <value>An int768.</value>
        public int AnInt768 { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean769].
        /// </summary>
        /// <value><c>true</c> if [a boolean769]; otherwise, <c>false</c>.</value>
        public bool CastsShadow { get; private set; }
        /// <summary>
        /// Gets an int770.
        /// </summary>
        /// <value>An int770.</value>
        public int OffsetZ { get; private set; }
        /// <summary>
        /// Gets an int773.
        /// </summary>
        /// <value>An int773.</value>
        public int AnInt773 { get; private set; }
        /// <summary>
        /// Gets an int array774.
        /// </summary>
        /// <value>An int array774.</value>
        public int[]? TransformToIDs { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean775].
        /// </summary>
        /// <value><c>true</c> if [a boolean775]; otherwise, <c>false</c>.</value>
        public bool DelayShading { get; private set; }
        /// <summary>
        /// Gets an int776.
        /// </summary>
        /// <value>An int776.</value>
        public int AnInt776 { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean777].
        /// </summary>
        /// <value><c>true</c> if [a boolean777]; otherwise, <c>false</c>.</value>
        public bool Hidden { get; private set; }
        /// <summary>
        /// Gets an int778.
        /// </summary>
        /// <value>An int778.</value>
        public int AnInt778 { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean779].
        /// </summary>
        /// <value><c>true</c> if [a boolean779]; otherwise, <c>false</c>.</value>
        public bool ABoolean779 { get; private set; }
        /// <summary>
        /// Gets an int780.
        /// </summary>
        /// <value>An int780.</value>
        public int AnInt780 { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean781].
        /// </summary>
        /// <value><c>true</c> if [a boolean781]; otherwise, <c>false</c>.</value>
        public bool ABoolean781 { get; private set; }
        /// <summary>
        /// Gets an int782.
        /// </summary>
        /// <value>An int782.</value>
        public int AnInt782 { get; private set; }
        /// <summary>
        /// Gets an int array783.
        /// </summary>
        /// <value>An int array783.</value>
        public int[]? QuestIDs { get; private set; }
        /// <summary>
        /// Gets an int array784.
        /// </summary>
        /// <value>An int array784.</value>
        public int[]? AnIntArray784 { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [members only].
        /// </summary>
        /// <value><c>true</c> if [members only]; otherwise, <c>false</c>.</value>
        public bool MembersOnly { get; private set; }
        /// <summary>
        /// Gets an int786.
        /// </summary>
        /// <value>An int786.</value>
        public int ScaleY { get; private set; }
        /// <summary>
        /// Gets a byte787.
        /// </summary>
        /// <value>A byte787.</value>
        public sbyte AByte787 { get; private set; }
        /// <summary>
        /// Gets an int788.
        /// </summary>
        /// <value>An int788.</value>
        public int AnInt788 { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean789].
        /// </summary>
        /// <value><c>true</c> if [a boolean789]; otherwise, <c>false</c>.</value>
        public bool ObstructsGround { get; private set; }
        /// <summary>
        /// Gets a byte790.
        /// </summary>
        /// <value>A byte790.</value>
        public sbyte AByte790 { get; private set; }
        /// <summary>
        /// Gets a short array791.
        /// </summary>
        /// <value>A short array791.</value>
        public short[]? OriginalColors { get; private set; }
        /// <summary>
        /// Gets an int792.
        /// </summary>
        /// <value>An int792.</value>
        public int ScaleX { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean793].
        /// </summary>
        /// <value><c>true</c> if [a boolean793]; otherwise, <c>false</c>.</value>
        public bool AdjustMapSceneRotation { get; private set; }
        /// <summary>
        /// Gets an int794.
        /// </summary>
        /// <value>An int794.</value>
        public int OffsetX { get; private set; }
        /// <summary>
        /// Gets the actions.
        /// </summary>
        /// <value>The actions.</value>
        public string?[] Actions { get; private set; }
        /// <summary>
        /// Gets the animation IDS.
        /// </summary>
        /// <value>The animation IDS.</value>
        public int[]? AnimationIDs { get; private set; }
        /// <summary>
        /// Gets a byte798.
        /// </summary>
        /// <value>A byte798.</value>
        public sbyte GroundContoured { get; private set; }
        /// <summary>
        /// Gets an int799.
        /// </summary>
        /// <value>An int799.</value>
        public int Contrast { get; private set; }
        /// <summary>
        /// Gets an int800.
        /// </summary>
        /// <value>An int800.</value>
        public int VarpBitFileId { get; private set; }
        /// <summary>
        /// Gets a value indicating whether this <see cref="ObjectType" /> is solid.
        /// </summary>
        /// <value><c>true</c> if solid; otherwise, <c>false</c>.</value>
        public bool Solid { get; private set; }
        /// <summary>
        /// Gets a byte array802.
        /// </summary>
        /// <value>A byte array802.</value>
        public sbyte[]? Shapes { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean803].
        /// </summary>
        /// <value><c>true</c> if [a boolean803]; otherwise, <c>false</c>.</value>
        public bool HasAnimation { get; private set; }
        /// <summary>
        /// Gets an int804.
        /// </summary>
        /// <value>An int804.</value>
        public int SupportItemsFlag { get; private set; }
        /// <summary>
        /// Gets an int805.
        /// </summary>
        /// <value>An int805.</value>
        public int Interactable { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean806].
        /// </summary>
        /// <value><c>true</c> if [a boolean806]; otherwise, <c>false</c>.</value>
        public bool FlipMapSprite { get; private set; }
        /// <summary>
        /// Gets an int807.
        /// </summary>
        /// <value>An int807.</value>
        public int Ambient { get; private set; }
        /// <summary>
        /// Gets an int808.
        /// </summary>
        /// <value>An int808.</value>
        public int AnInt808 { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean810].
        /// </summary>
        /// <value><c>true</c> if [a boolean810]; otherwise, <c>false</c>.</value>
        public bool ABoolean810 { get; private set; }
        /// <summary>
        /// Gets an int811.
        /// </summary>
        /// <value>An int811.</value>
        public int AmbientSoundID { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean812].
        /// </summary>
        /// <value><c>true</c> if [a boolean812]; otherwise, <c>false</c>.</value>
        public bool Inverted { get; private set; }
        /// <summary>
        /// Gets an int813.
        /// </summary>
        /// <value>An int813.</value>
        public int AnInt813 { get; private set; }
        /// <summary>
        /// Gets an int array array814.
        /// </summary>
        /// <value>An int array array814.</value>
        public int[][] ModelIDs { get; private set; }
        /// <summary>
        /// Gets an int815.
        /// </summary>
        /// <value>An int815.</value>
        public int AmbientSoundVolume { get; private set; }
        /// <summary>
        /// Gets a byte array816.
        /// </summary>
        /// <value>A byte array816.</value>
        public sbyte[] AByteArray816 { get; private set; }
        /// <summary>
        /// Gets the size Y.
        /// </summary>
        /// <value>The size Y.</value>
        public int SizeY { get; private set; }
        /// <summary>
        /// Gets a short array819.
        /// </summary>
        /// <value>A short array819.</value>
        public short[]? ModifiedColors { get; private set; }
        /// <summary>
        /// Gets the size X.
        /// </summary>
        /// <value>The size X.</value>
        public int SizeX { get; private set; }
        /// <summary>
        /// Gets a byte821.
        /// </summary>
        /// <value>A byte821.</value>
        public sbyte AByte821 { get; private set; }
        /// <summary>
        /// Gets an int822.
        /// </summary>
        /// <value>An int822.</value>
        public int DecorDisplacement { get; private set; }
        /// <summary>
        /// Gets an int823.
        /// </summary>
        /// <value>An int823.</value>
        public int AnInt823 { get; private set; }
        /// <summary>
        /// Gets the type of the clip.
        /// </summary>
        /// <value>The type of the clip.</value>
        public int ClipType { get; private set; }
        /// <summary>
        /// Gets an int825.
        /// </summary>
        /// <value>An int825.</value>
        public int AnInt825 { get; private set; }
        /// <summary>
        /// Gets a byte826.
        /// </summary>
        /// <value>A byte826.</value>
        public sbyte AByte826 { get; private set; }
        /// <summary>
        /// Gets an int827.
        /// </summary>
        /// <value>An int827.</value>
        public int AnInt827 { get; private set; }
        /// <summary>
        /// Gets an int828.
        /// </summary>
        /// <value>An int828.</value>
        public int AnInt828 { get; private set; }
        /// <summary>
        /// Gets a value indicating whether this <see cref="ObjectType" /> is walkable.
        /// </summary>
        /// <value><c>true</c> if walkable; otherwise, <c>false</c>.</value>
        public bool Gateway { get; private set; }
        /// <summary>
        /// Gets an int830.
        /// </summary>
        /// <value>An int830.</value>
        public int AnInt830 { get; private set; }
        /// <summary>
        /// Gets a short array831.
        /// </summary>
        /// <value>A short array831.</value>
        public short[]? AShortArray831 { get; private set; }
        /// <summary>
        /// Gets an int832.
        /// </summary>
        /// <value>An int832.</value>
        public int Occludes { get; private set; }
        /// <summary>
        /// Gets an int833.
        /// </summary>
        /// <value>An int833.</value>
        public int AnInt833 { get; private set; }
        /// <summary>
        /// Gets an int834.
        /// </summary>
        /// <value>An int834.</value>
        public int VarpFileId { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean835].
        /// </summary>
        /// <value><c>true</c> if [a boolean835]; otherwise, <c>false</c>.</value>
        public bool ABoolean835 { get; private set; }
        /// <summary>
        /// Gets an int836.
        /// </summary>
        /// <value>An int836.</value>
        public int ScaleZ { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean838].
        /// </summary>
        /// <value><c>true</c> if [a boolean838]; otherwise, <c>false</c>.</value>
        public bool ABoolean838 { get; private set; }
        /// <summary>
        /// Gets the surroundings.
        /// </summary>
        /// <value>
        /// The surroundings.
        /// </value>
        public byte Surroundings { get; private set; }

        /// <summary>
        /// Construct's new object definition with given ID.
        /// </summary>
        /// <param name="id">ID of the object.</param>
        public ObjectType(int id)
        {
            Id = id;
            Actions = new string?[] { null, null, null, null, null, "Examine" };
            ScaleY = 128;
            ABoolean781 = false;
            MapSpriteRotation = 0;
            DelayShading = false;
            AnInt764 = -1;
            Interactable = -1;
            Solid = true;
            AnInt776 = 0;
            OffsetX = 0;
            AmbientSoundID = -1;
            MembersOnly = false;
            Contrast = 0;
            ObstructsGround = false;
            AmbientSoundHearDistance = 0;
            AnInt788 = -1;
            ABoolean810 = false;
            MapIcon = -1;
            ScaleX = 128;
            FlipMapSprite = false;
            AnInt813 = 0;
            OffsetZ = 0;
            AnInt780 = -1;
            AnInt808 = 256;
            AnIntArray784 = null;
            AnInt778 = 0;
            GroundContoured = (sbyte)0;
            AdjustMapSceneRotation = false;
            OffsetY = 0;
            AnimationIDs = null;
            AnInt823 = 960;
            VarpBitFileId = -1;
            SizeY = 1;
            AnInt825 = 256;
            Surroundings = 0;
            MapSpriteType = -1;
            DecorDisplacement = 64;
            AnInt828 = -1;
            Name = "null";
            Inverted = false;
            Gateway = false;
            Occludes = -1;
            ClipType = 2;
            CastsShadow = true;
            AnInt833 = 0;
            AByte787 = (sbyte)0;
            Ambient = 0;
            SupportItemsFlag = -1;
            Hidden = false;
            ABoolean835 = false;
            HasAnimation = false;
            AnInt827 = -1;
            SizeX = 1;
            ABoolean779 = true;
            AmbientSoundVolume = 255;
            AnInt830 = 0;
            ABoolean838 = true;
            VarpFileId = -1;
            ScaleZ = 128;
            ModelIDs = Array.Empty<int[]>();
            AByteArray816 = Array.Empty<sbyte>();
        }

        /// <summary>
        /// Set's something in definition.
        /// </summary>
        public void AfterDecode()
        {
            if (Interactable == -1)
            {
                Interactable = 0;
                if (Shapes != null && Shapes.Length == 1 && Shapes[0] == 10)
                {
                    Interactable = 1;
                }
                for (int i = 0; i < 5; i++)
                {
                    if (Actions[i] != null)
                    {
                        Interactable = 1;
                        break;
                    }
                }
            }
            if (SupportItemsFlag == -1)
            {
                SupportItemsFlag = ClipType == 0 ? 0 : 1;
            }
            if (AnimationIDs != null || HasAnimation || TransformToIDs != null)
            {
                ABoolean835 = true;
            }
            if (Gateway)
            {
                ClipType = 0;
                Solid = false;
            }
            //if (ClipType == 2)
            //{
                //Solid = true;
            //}
        }

        public void Decode(MemoryStream buffer)
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
                                SizeY = buffer.ReadUnsignedByte();
                            else if (opcode != 17)
                            {
                                if (opcode == 18)
                                    Solid = false;
                                else if (opcode != 19)
                                {
                                    if (opcode == 21)
                                        GroundContoured = 1;
                                    else if (opcode == 22)
                                        DelayShading = true;
                                    else if (opcode != 23)
                                    {
                                        if (opcode == 24)
                                        {
                                            int animationID = buffer.ReadBigSmart();
                                            if (animationID != 65535)
                                                AnimationIDs = new int[] { animationID };
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
                                                            Actions[opcode - 30] = (buffer.ReadString());
                                                        else if (opcode != 40)
                                                        {
                                                            if (opcode == 41)
                                                            {
                                                                int i20 = (buffer.ReadUnsignedByte());
                                                                AShortArray831 = new short[i20];
                                                                AShortArray762 = new short[i20];
                                                                for (int i21 = 0; i20 > i21; i21++)
                                                                {
                                                                    AShortArray831[i21] = (short)(buffer.ReadUnsignedShort());
                                                                    AShortArray762[i21] = (short)(buffer.ReadUnsignedShort());
                                                                }
                                                            }
                                                            else if (opcode != 42)
                                                            {
                                                                if (opcode == 44)
                                                                {
                                                                    int i16 = buffer.ReadUnsignedShort();
                                                                    /*int i_17_ = 0;
                                                                    for (int i_18_ = i_16_; i_18_ > 0; i_18_ >>= 1)
                                                                        i_17_++;
                                                                    sbyte[] aByteArray5590 = new sbyte[i_17_];
                                                                    sbyte i_19_ = 0;
                                                                    for (int i_20_ = 0; i_20_ < i_17_; i_20_++)
                                                                    {
                                                                        if ((i_16_ & 1 << i_20_) > 0)
                                                                        {
                                                                            aByteArray5590[i_20_] = i_19_;
                                                                            i_19_++;
                                                                        }
                                                                        else
                                                                            aByteArray5590[i_20_] = (sbyte)-1;
                                                                    }*/
                                                                }
                                                                else if (opcode == 45)
                                                                {
                                                                    int i21 = buffer.ReadUnsignedShort();
                                                                    /*int i_22_ = 0;
                                                                    for (int i_23_ = i_21_; i_23_ > 0; i_23_ >>= 1)
                                                                        i_22_++;
                                                                    sbyte[] aByteArray5581 = new sbyte[i_22_];
                                                                    sbyte i_24_ = 0;
                                                                    for (int i_25_ = 0; i_25_ < i_22_; i_25_++)
                                                                    {
                                                                        if ((i_21_ & 1 << i_25_) > 0)
                                                                        {
                                                                            aByteArray5581[i_25_] = i_24_;
                                                                            i_24_++;
                                                                        }
                                                                        else
                                                                            aByteArray5581[i_25_] = (sbyte)-1;
                                                                    }*/
                                                                }
                                                                else if (opcode == 62)
                                                                    Inverted = true;
                                                                else if (opcode == 64)
                                                                    CastsShadow = false;
                                                                else if (opcode != 65)
                                                                {
                                                                    if (opcode == 66)
                                                                        ScaleY = (buffer.ReadUnsignedShort());
                                                                    else if (opcode == 67)
                                                                        ScaleZ = (buffer.ReadUnsignedShort());
                                                                    else if (opcode != 69)
                                                                    {
                                                                        if (opcode == 70)
                                                                            OffsetX = ((buffer.ReadShort()) << 2);
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
                                                                                                                    ABoolean779 = false;
                                                                                                                else if (opcode != 89)
                                                                                                                {
                                                                                                                    if (opcode != 91)
                                                                                                                    {
                                                                                                                        if (opcode == 93)
                                                                                                                        {
                                                                                                                            GroundContoured = (sbyte)3;
                                                                                                                            AnInt780 = buffer.ReadUnsignedShort();
                                                                                                                        }
                                                                                                                        else if (opcode == 94)
                                                                                                                            GroundContoured = (sbyte)4;
                                                                                                                        else if (opcode != 95)
                                                                                                                        {
                                                                                                                            if (opcode != 97)
                                                                                                                            {
                                                                                                                                if (opcode == 98)
                                                                                                                                    HasAnimation = true;
                                                                                                                                else if (opcode != 99)
                                                                                                                                {
                                                                                                                                    if (opcode == 100)
                                                                                                                                    {
                                                                                                                                        AnInt764 = buffer.ReadUnsignedByte();
                                                                                                                                        AnInt828 = buffer.ReadUnsignedShort();
                                                                                                                                    }
                                                                                                                                    else if (opcode != 101)
                                                                                                                                    {
                                                                                                                                        if (opcode == 102)
                                                                                                                                            MapSpriteType = buffer.ReadUnsignedShort();
                                                                                                                                        else if (opcode != 103)
                                                                                                                                        {
                                                                                                                                            if (opcode == 104)
                                                                                                                                                AmbientSoundVolume = buffer.ReadUnsignedByte();
                                                                                                                                            else if (opcode == 105)
                                                                                                                                                FlipMapSprite = true;
                                                                                                                                            else if (opcode == 106)
                                                                                                                                            {
                                                                                                                                                int i22 = buffer.ReadUnsignedByte();
                                                                                                                                                int i23 = 0;
                                                                                                                                                AnIntArray784 = new int[i22];
                                                                                                                                                AnimationIDs = new int[i22];
                                                                                                                                                for (int i24 = 0; i22 > i24; i24++)
                                                                                                                                                {
                                                                                                                                                    AnimationIDs[i24] = buffer.ReadBigSmart();
                                                                                                                                                    if (AnimationIDs[i24] == 65535)
                                                                                                                                                        AnimationIDs[i24] = -1;
                                                                                                                                                    i23 += AnIntArray784[i24] = buffer.ReadUnsignedByte();
                                                                                                                                                }
                                                                                                                                                for (int i25 = 0; i22 > i25; i25++)
                                                                                                                                                    AnIntArray784[i25] = AnIntArray784[i25] * 65535 / i23;
                                                                                                                                            }
                                                                                                                                            else if (opcode != 107)
                                                                                                                                            {
                                                                                                                                                if (opcode < 150 || opcode >= 155)
                                                                                                                                                {
                                                                                                                                                    if (opcode == 160)
                                                                                                                                                    {
                                                                                                                                                        int i26 = buffer.ReadUnsignedByte();
                                                                                                                                                        QuestIDs = new int[i26];
                                                                                                                                                        for (int i27 = 0; i27 < i26; i27++)
                                                                                                                                                            QuestIDs[i27] = buffer.ReadUnsignedShort();
                                                                                                                                                    }
                                                                                                                                                    else if (opcode != 162)
                                                                                                                                                    {
                                                                                                                                                        if (opcode == 163)
                                                                                                                                                        {
                                                                                                                                                            AByte826 = (sbyte)buffer.ReadSignedByte();
                                                                                                                                                            AByte790 = (sbyte)buffer.ReadSignedByte();
                                                                                                                                                            AByte821 = (sbyte)buffer.ReadSignedByte();
                                                                                                                                                            AByte787 = (sbyte)buffer.ReadSignedByte();
                                                                                                                                                        }
                                                                                                                                                        else if (opcode != 164)
                                                                                                                                                        {
                                                                                                                                                            if (opcode == 165)
                                                                                                                                                                AnInt830 = buffer.ReadShort();
                                                                                                                                                            else if (opcode == 166)
                                                                                                                                                                AnInt778 = buffer.ReadShort();
                                                                                                                                                            else if (opcode == 167)
                                                                                                                                                                AnInt776 = buffer.ReadUnsignedShort();
                                                                                                                                                            else if (opcode == 168)
                                                                                                                                                                ABoolean810 = true;
                                                                                                                                                            else if (opcode == 169)
                                                                                                                                                                ABoolean781 = true;
                                                                                                                                                            else if (opcode != 170)
                                                                                                                                                            {
                                                                                                                                                                if (opcode != 171)
                                                                                                                                                                {
                                                                                                                                                                    if (opcode == 173)
                                                                                                                                                                    {
                                                                                                                                                                        AnInt825 = buffer.ReadUnsignedShort();
                                                                                                                                                                        AnInt808 = buffer.ReadUnsignedShort();
                                                                                                                                                                    }
                                                                                                                                                                    else if (opcode == 177)
                                                                                                                                                                        ABoolean835 = true;
                                                                                                                                                                    else if (opcode != 178)
                                                                                                                                                                    {
                                                                                                                                                                        if (opcode == 189)
                                                                                                                                                                        {
                                                                                                                                                                            //bool somebool = true; // bloom?
                                                                                                                                                                        }
                                                                                                                                                                        else if (opcode >= 190 && opcode < 196)
                                                                                                                                                                        {
                                                                                                                                                                            buffer.ReadUnsignedShort();
                                                                                                                                                                        }
                                                                                                                                                                        else if (opcode == 249)
                                                                                                                                                                        {
                                                                                                                                                                            int count = buffer.ReadUnsignedByte();
                                                                                                                                                                            ExtraData = new Dictionary<int, object>(count);
                                                                                                                                                                            for (int i = 0; count > i; i++)
                                                                                                                                                                            {
                                                                                                                                                                                bool isStr = buffer.ReadUnsignedByte() == 1;
                                                                                                                                                                                int key = buffer.ReadMedInt();
                                                                                                                                                                                if (!isStr)
                                                                                                                                                                                    ExtraData.Add(key, buffer.ReadInt());
                                                                                                                                                                                else
                                                                                                                                                                                    ExtraData.Add(key, buffer.ReadString());
                                                                                                                                                                            }
                                                                                                                                                                        }
                                                                                                                                                                    }
                                                                                                                                                                    else
                                                                                                                                                                        AnInt813 = buffer.ReadUnsignedByte();
                                                                                                                                                                }
                                                                                                                                                                else
                                                                                                                                                                    AnInt773 = buffer.ReadSmart();
                                                                                                                                                            }
                                                                                                                                                            else
                                                                                                                                                                AnInt823 = buffer.ReadSmart();
                                                                                                                                                        }
                                                                                                                                                        else
                                                                                                                                                            AnInt782 = buffer.ReadShort();
                                                                                                                                                    }
                                                                                                                                                    else
                                                                                                                                                    {
                                                                                                                                                        GroundContoured = (sbyte)3;
                                                                                                                                                        AnInt780 = buffer.ReadInt();
                                                                                                                                                    }
                                                                                                                                                }
                                                                                                                                                else
                                                                                                                                                {
                                                                                                                                                    Actions[opcode - 150] = buffer.ReadString();
                                                                                                                                                }
                                                                                                                                            }
                                                                                                                                            else
                                                                                                                                                MapIcon = buffer.ReadUnsignedShort();
                                                                                                                                        }
                                                                                                                                        else
                                                                                                                                            Occludes = 0;
                                                                                                                                    }
                                                                                                                                    else
                                                                                                                                        MapSpriteRotation = buffer.ReadUnsignedByte();
                                                                                                                                }
                                                                                                                                else
                                                                                                                                {
                                                                                                                                    AnInt788 = buffer.ReadUnsignedByte();
                                                                                                                                    AnInt827 = buffer.ReadUnsignedShort();
                                                                                                                                }
                                                                                                                            }
                                                                                                                            else
                                                                                                                                AdjustMapSceneRotation = true;
                                                                                                                        }
                                                                                                                        else
                                                                                                                        {
                                                                                                                            GroundContoured = (sbyte)5;
                                                                                                                            AnInt780 = buffer.ReadShort();
                                                                                                                        }
                                                                                                                    }
                                                                                                                    else
                                                                                                                        MembersOnly = true;
                                                                                                                }
                                                                                                                else
                                                                                                                    ABoolean838 = false;
                                                                                                            }
                                                                                                            else
                                                                                                                Hidden = true;
                                                                                                        }
                                                                                                        else
                                                                                                        {
                                                                                                            GroundContoured = (sbyte)2;
                                                                                                            AnInt780 = buffer.ReadUnsignedByte() * 256;
                                                                                                        }
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        AnInt833 = buffer.ReadUnsignedShort();
                                                                                                        AnInt768 = buffer.ReadUnsignedShort();
                                                                                                        AmbientSoundHearDistance = buffer.ReadUnsignedByte();
                                                                                                        int i32 = buffer.ReadUnsignedByte();
                                                                                                        AudioTracks = new int[i32];
                                                                                                        for (int i33 = 0; i32 > i33; i33++)
                                                                                                            AudioTracks[i33] = buffer.ReadUnsignedShort();
                                                                                                    }
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    AmbientSoundID = buffer.ReadUnsignedShort();
                                                                                                    AmbientSoundHearDistance = buffer.ReadUnsignedByte();
                                                                                                }
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                VarpBitFileId = buffer.ReadUnsignedShort();
                                                                                                if (VarpBitFileId == 65535)
                                                                                                    VarpBitFileId = -1;
                                                                                                VarpFileId = buffer.ReadUnsignedShort();
                                                                                                if (VarpFileId == 65535)
                                                                                                    VarpFileId = -1;
                                                                                                int i34 = -1;
                                                                                                if (opcode == 92)
                                                                                                {
                                                                                                    i34 = buffer.ReadBigSmart();
                                                                                                }
                                                                                                int childrenCount = buffer.ReadUnsignedByte();
                                                                                                TransformToIDs = new int[childrenCount + 2];
                                                                                                for (int childIndex = 0; childIndex <= childrenCount; childIndex++)
                                                                                                {
                                                                                                    TransformToIDs[childIndex] = buffer.ReadBigSmart();
                                                                                                }
                                                                                                TransformToIDs[childrenCount + 1] = i34;
                                                                                            }
                                                                                        }
                                                                                        else
                                                                                            SupportItemsFlag = buffer.ReadUnsignedByte();
                                                                                    }
                                                                                    else
                                                                                        Gateway = true;
                                                                                }
                                                                                else
                                                                                    ObstructsGround = true;
                                                                            }
                                                                            else
                                                                                OffsetZ = buffer.ReadShort() << 2;
                                                                        }
                                                                        else
                                                                            OffsetY = ((buffer.ReadShort()) << 2);
                                                                    }
                                                                    else
                                                                        Surroundings = (byte)buffer.ReadUnsignedByte();
                                                                }
                                                                else
                                                                    ScaleX = (buffer.ReadUnsignedShort());
                                                            }
                                                            else
                                                            {
                                                                int i37 = (buffer.ReadUnsignedByte());
                                                                AByteArray816 = new sbyte[i37];
                                                                for (int i38 = 0; i37 > i38; i38++)
                                                                    AByteArray816[i38] = ((sbyte)buffer.ReadSignedByte());
                                                            }
                                                        }
                                                        else
                                                        {
                                                            int i39 = (buffer.ReadUnsignedByte());
                                                            ModifiedColors = new short[i39];
                                                            OriginalColors = new short[i39];
                                                            for (int i40 = 0; i39 > i40; i40++)
                                                            {
                                                                OriginalColors[i40] = (short)(buffer.ReadUnsignedShort());
                                                                ModifiedColors[i40] = (short)(buffer.ReadUnsignedShort());
                                                            }
                                                        }
                                                    }
                                                    else
                                                        Contrast = (sbyte)buffer.ReadSignedByte() * 5;
                                                }
                                                else
                                                    Ambient = (sbyte)buffer.ReadSignedByte();
                                            }
                                            else
                                                DecorDisplacement = (buffer.ReadUnsignedByte() << 2);
                                        }
                                        else
                                            ClipType = 1;
                                    }
                                    else
                                        Occludes = 1;
                                }
                                else
                                    Interactable = buffer.ReadUnsignedByte();
                            }
                            else
                            {
                                ClipType = 0;
                                Solid = false;
                            }
                        }
                        else
                            SizeX = buffer.ReadUnsignedByte();
                    }
                    else
                        Name = buffer.ReadString();
                }
                else
                {
                    int i41 = buffer.ReadUnsignedByte();
                    Shapes = new sbyte[i41];
                    ModelIDs = new int[i41][];
                    for (int i42 = 0; i41 > i42; i42++)
                    {
                        Shapes[i42] = (sbyte)buffer.ReadSignedByte();
                        int i43 = buffer.ReadUnsignedByte();
                        ModelIDs[i42] = new int[i43];
                        for (int i44 = 0; i44 < i43; i44++)
                            ModelIDs[i42][i44] = buffer.ReadBigSmart();
                    }
                }
            }
        }
        public MemoryStream Encode() => throw new System.NotImplementedException();
    }
}
