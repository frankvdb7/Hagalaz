using Hagalaz.Cache.Abstractions.Types;
using System;
using System.Collections.Generic;
using System.IO;

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
        public Dictionary<int, object>? ExtraData { get; internal set; }
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
        public int MapIcon { get; internal set; }
        /// <summary>
        /// Gets an int760.
        /// </summary>
        /// <value>An int760.</value>
        public int OffsetY { get; internal set; }
        /// <summary>
        /// Gets an int761.
        /// </summary>
        /// <value>An int761.</value>
        public int AmbientSoundHearDistance { get; internal set; }
        /// <summary>
        /// Gets a short array762.
        /// </summary>
        /// <value>A short array762.</value>
        public short[]? AShortArray762 { get; internal set; }
        /// <summary>
        /// Gets an int763.
        /// </summary>
        /// <value>An int763.</value>
        public int MapSpriteRotation { get; internal set; }
        /// <summary>
        /// Gets an int764.
        /// </summary>
        /// <value>An int764.</value>
        public int AnInt764 { get; internal set; }
        /// <summary>
        /// Gets an int765.
        /// </summary>
        /// <value>An int765.</value>
        public int MapSpriteType { get; internal set; }
        /// <summary>
        /// Gets an int array766.
        /// </summary>
        /// <value>An int array766.</value>
        public int[]? AudioTracks { get; internal set; }
        /// <summary>
        /// Gets an int768.
        /// </summary>
        /// <value>An int768.</value>
        public int AnInt768 { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean769].
        /// </summary>
        /// <value><c>true</c> if [a boolean769]; otherwise, <c>false</c>.</value>
        public bool CastsShadow { get; internal set; }
        /// <summary>
        /// Gets an int770.
        /// </summary>
        /// <value>An int770.</value>
        public int OffsetZ { get; internal set; }
        /// <summary>
        /// Gets an int773.
        /// </summary>
        /// <value>An int773.</value>
        public int AnInt773 { get; internal set; }
        /// <summary>
        /// Gets an int array774.
        /// </summary>
        /// <value>An int array774.</value>
        public int[]? TransformToIDs { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean775].
        /// </summary>
        /// <value><c>true</c> if [a boolean775]; otherwise, <c>false</c>.</value>
        public bool DelayShading { get; internal set; }
        /// <summary>
        /// Gets an int776.
        /// </summary>
        /// <value>An int776.</value>
        public int AnInt776 { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean777].
        /// </summary>
        /// <value><c>true</c> if [a boolean777]; otherwise, <c>false</c>.</value>
        public bool Hidden { get; internal set; }
        /// <summary>
        /// Gets an int778.
        /// </summary>
        /// <value>An int778.</value>
        public int AnInt778 { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean779].
        /// </summary>
        /// <value><c>true</c> if [a boolean779]; otherwise, <c>false</c>.</value>
        public bool ABoolean779 { get; internal set; }
        /// <summary>
        /// Gets an int780.
        /// </summary>
        /// <value>An int780.</value>
        public int AnInt780 { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean781].
        /// </summary>
        /// <value><c>true</c> if [a boolean781]; otherwise, <c>false</c>.</value>
        public bool ABoolean781 { get; internal set; }
        /// <summary>
        /// Gets an int782.
        /// </summary>
        /// <value>An int782.</value>
        public int AnInt782 { get; internal set; }
        /// <summary>
        /// Gets an int array783.
        /// </summary>
        /// <value>An int array783.</value>
        public int[]? QuestIDs { get; internal set; }
        /// <summary>
        /// Gets an int array784.
        /// </summary>
        /// <value>An int array784.</value>
        public int[]? AnIntArray784 { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [members only].
        /// </summary>
        /// <value><c>true</c> if [members only]; otherwise, <c>false</c>.</value>
        public bool MembersOnly { get; internal set; }
        /// <summary>
        /// Gets an int786.
        /// </summary>
        /// <value>An int786.</value>
        public int ScaleY { get; internal set; }
        /// <summary>
        /// Gets a byte787.
        /// </summary>
        /// <value>A byte787.</value>
        public sbyte AByte787 { get; internal set; }
        /// <summary>
        /// Gets an int788.
        /// </summary>
        /// <value>An int788.</value>
        public int AnInt788 { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean789].
        /// </summary>
        /// <value><c>true</c> if [a boolean789]; otherwise, <c>false</c>.</value>
        public bool ObstructsGround { get; internal set; }
        /// <summary>
        /// Gets a byte790.
        /// </summary>
        /// <value>A byte790.</value>
        public sbyte AByte790 { get; internal set; }
        /// <summary>
        /// Gets a short array791.
        /// </summary>
        /// <value>A short array791.</value>
        public short[]? OriginalColors { get; internal set; }
        /// <summary>
        /// Gets an int792.
        /// </summary>
        /// <value>An int792.</value>
        public int ScaleX { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean793].
        /// </summary>
        /// <value><c>true</c> if [a boolean793]; otherwise, <c>false</c>.</value>
        public bool AdjustMapSceneRotation { get; internal set; }
        /// <summary>
        /// Gets an int794.
        /// </summary>
        /// <value>An int794.</value>
        public int OffsetX { get; internal set; }
        /// <summary>
        /// Gets the actions.
        /// </summary>
        /// <value>The actions.</value>
        public string?[] Actions { get; internal set; }
        /// <summary>
        /// Gets the animation IDS.
        /// </summary>
        /// <value>The animation IDS.</value>
        public int[]? AnimationIDs { get; internal set; }
        /// <summary>
        /// Gets a byte798.
        /// </summary>
        /// <value>A byte798.</value>
        public sbyte GroundContoured { get; internal set; }
        /// <summary>
        /// Gets an int799.
        /// </summary>
        /// <value>An int799.</value>
        public int Contrast { get; internal set; }
        /// <summary>
        /// Gets an int800.
        /// </summary>
        /// <value>An int800.</value>
        public int VarpBitFileId { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether this <see cref="ObjectType" /> is solid.
        /// </summary>
        /// <value><c>true</c> if solid; otherwise, <c>false</c>.</value>
        public bool Solid { get; internal set; }
        /// <summary>
        /// Gets a byte array802.
        /// </summary>
        /// <value>A byte array802.</value>
        public sbyte[]? Shapes { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean803].
        /// </summary>
        /// <value><c>true</c> if [a boolean803]; otherwise, <c>false</c>.</value>
        public bool HasAnimation { get; internal set; }
        /// <summary>
        /// Gets an int804.
        /// </summary>
        /// <value>An int804.</value>
        public int SupportItemsFlag { get; internal set; }
        /// <summary>
        /// Gets an int805.
        /// </summary>
        /// <value>An int805.</value>
        public int Interactable { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean806].
        /// </summary>
        /// <value><c>true</c> if [a boolean806]; otherwise, <c>false</c>.</value>
        public bool FlipMapSprite { get; internal set; }
        /// <summary>
        /// Gets an int807.
        /// </summary>
        /// <value>An int807.</value>
        public int Ambient { get; internal set; }
        /// <summary>
        /// Gets an int808.
        /// </summary>
        /// <value>An int808.</value>
        public int AnInt808 { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean810].
        /// </summary>
        /// <value><c>true</c> if [a boolean810]; otherwise, <c>false</c>.</value>
        public bool ABoolean810 { get; internal set; }
        /// <summary>
        /// Gets an int811.
        /// </summary>
        /// <value>An int811.</value>
        public int AmbientSoundID { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean812].
        /// </summary>
        /// <value><c>true</c> if [a boolean812]; otherwise, <c>false</c>.</value>
        public bool Inverted { get; internal set; }
        /// <summary>
        /// Gets an int813.
        /// </summary>
        /// <value>An int813.</value>
        public int AnInt813 { get; internal set; }
        /// <summary>
        /// Gets an int array array814.
        /// </summary>
        /// <value>An int array array814.</value>
        public int[][] ModelIDs { get; internal set; }
        /// <summary>
        /// Gets an int815.
        /// </summary>
        /// <value>An int815.</value>
        public int AmbientSoundVolume { get; internal set; }
        /// <summary>
        /// Gets a byte array816.
        /// </summary>
        /// <value>A byte array816.</value>
        public sbyte[] AByteArray816 { get; internal set; }
        /// <summary>
        /// Gets the size Y.
        /// </summary>
        /// <value>The size Y.</value>
        public int SizeY { get; internal set; }
        /// <summary>
        /// Gets a short array819.
        /// </summary>
        /// <value>A short array819.</value>
        public short[]? ModifiedColors { get; internal set; }
        /// <summary>
        /// Gets the size X.
        /// </summary>
        /// <value>The size X.</value>
        public int SizeX { get; internal set; }
        /// <summary>
        /// Gets a byte821.
        /// </summary>
        /// <value>A byte821.</value>
        public sbyte AByte821 { get; internal set; }
        /// <summary>
        /// Gets an int822.
        /// </summary>
        /// <value>An int822.</value>
        public int DecorDisplacement { get; internal set; }
        /// <summary>
        /// Gets an int823.
        /// </summary>
        /// <value>An int823.</value>
        public int AnInt823 { get; internal set; }
        /// <summary>
        /// Gets the type of the clip.
        /// </summary>
        /// <value>The type of the clip.</value>
        public int ClipType { get; internal set; }
        /// <summary>
        /// Gets an int825.
        /// </summary>
        /// <value>An int825.</value>
        public int AnInt825 { get; internal set; }
        /// <summary>
        /// Gets a byte826.
        /// </summary>
        /// <value>A byte826.</value>
        public sbyte AByte826 { get; internal set; }
        /// <summary>
        /// Gets an int827.
        /// </summary>
        /// <value>An int827.</value>
        public int AnInt827 { get; internal set; }
        /// <summary>
        /// Gets an int828.
        /// </summary>
        /// <value>An int828.</value>
        public int AnInt828 { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether this <see cref="ObjectType" /> is walkable.
        /// </summary>
        /// <value><c>true</c> if walkable; otherwise, <c>false</c>.</value>
        public bool Gateway { get; internal set; }
        /// <summary>
        /// Gets an int830.
        /// </summary>
        /// <value>An int830.</value>
        public int AnInt830 { get; internal set; }
        /// <summary>
        /// Gets a short array831.
        /// </summary>
        /// <value>A short array831.</value>
        public short[]? AShortArray831 { get; internal set; }
        /// <summary>
        /// Gets an int832.
        /// </summary>
        /// <value>An int832.</value>
        public int Occludes { get; internal set; }
        /// <summary>
        /// Gets an int833.
        /// </summary>
        /// <value>An int833.</value>
        public int AnInt833 { get; internal set; }
        /// <summary>
        /// Gets an int834.
        /// </summary>
        /// <value>An int834.</value>
        public int VarpFileId { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean835].
        /// </summary>
        /// <value><c>true</c> if [a boolean835]; otherwise, <c>false</c>.</value>
        public bool ABoolean835 { get; internal set; }
        /// <summary>
        /// Gets an int836.
        /// </summary>
        /// <value>An int836.</value>
        public int ScaleZ { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean838].
        /// </summary>
        /// <value><c>true</c> if [a boolean838]; otherwise, <c>false</c>.</value>
        public bool ABoolean838 { get; internal set; }
        /// <summary>
        /// Gets the surroundings.
        /// </summary>
        /// <value>
        /// The surroundings.
        /// </value>
        public byte Surroundings { get; internal set; }

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
        /// Called after decode.
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
        }
    }
}