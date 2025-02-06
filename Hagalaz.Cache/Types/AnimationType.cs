using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// Represents creature animation definition.
    /// </summary>
    public class AnimationType : IAnimationType
    {
        /// <summary>
        /// Contains ID of this animation.
        /// </summary>
        /// <value>The ID.</value>
        public int Id { get; }
        /// <summary>
        /// Gets the extra data.
        /// </summary>
        /// <value>
        /// The extra data.
        /// </value>
        public Dictionary<int, object>? ExtraData { get; private set; }
        /// <summary>
        /// Gets an int399.
        /// </summary>
        /// <value>An int399.</value>
        public int Loop { get; private set; }
        /// <summary>
        /// Gets an int array400.
        /// </summary>
        /// <value>An int array400.</value>
        public int[]? MaxSoundSpeed { get; private set; }
        /// <summary>
        /// Gets the weapon displayed.
        /// </summary>
        /// <value>The weapon displayed.</value>
        public int WeaponDisplayedItemId { get; private set; }
        /// <summary>
        /// Gets an int array402.
        /// </summary>
        /// <value>An int array402.</value>
        public int[]? Frames { get; private set; }
        /// <summary>
        /// Gets an int403.
        /// </summary>
        /// <value>An int403.</value>
        public int Cycles { get; private set; }
        /// <summary>
        /// Gets an int404.
        /// </summary>
        /// <value>An int404.</value>
        public int AnimationMode { get; private set; }
        /// <summary>
        /// Gets an int array405.
        /// </summary>
        /// <value>An int array405.</value>
        public int[]? Delays { get; private set; }
        /// <summary>
        /// Gets a value indicating whether this instance is tweened.
        /// </summary>
        /// <value><c>true</c> if this instance is tweened; otherwise, <c>false</c>.</value>
        public bool IsTweened { get; private set; }
        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public int Priority { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean409].
        /// </summary>
        /// <value><c>true</c> if [a boolean409]; otherwise, <c>false</c>.</value>
        public bool ABoolean409 { get; private set; }
        /// <summary>
        /// Gets an int410.
        /// </summary>
        /// <value>An int410.</value>
        public int ReplayMode { get; private set; }
        /// <summary>
        /// Gets the shield displayed.
        /// </summary>
        /// <value>The shield displayed.</value>
        public int ShieldDisplayedItemId { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean413].
        /// </summary>
        /// <value><c>true</c> if [a boolean413]; otherwise, <c>false</c>.</value>
        public bool ABoolean413 { get; private set; }
        /// <summary>
        /// Gets a boolean array414.
        /// </summary>
        /// <value>A boolean array414.</value>
        public bool[]? ABooleanArray414 { get; private set; }
        /// <summary>
        /// Gets an int array415.
        /// </summary>
        /// <value>An int array415.</value>
        public int[]? MinSoundSpeed { get; private set; }
        /// <summary>
        /// Gets an int array416.
        /// </summary>
        /// <value>An int array416.</value>
        public int[]? SoundVolumes { get; private set; }
        /// <summary>
        /// Gets the walking properties.
        /// </summary>
        /// <value>The walking properties.</value>
        public int WalkingProperties { get; private set; }
        /// <summary>
        /// Gets an int array array418.
        /// </summary>
        /// <value>An int array array418.</value>
        public int[][]? SoundSettings { get; private set; }
        /// <summary>
        /// Gets an int array419.
        /// </summary>
        /// <value>An int array419.</value>
        public int[]? InterfaceFrames { get; private set; }

        /// <summary>
        /// Construct's new animation definition.
        /// </summary>
        /// <param name="id">ID of the animation.</param>
        public AnimationType(int id)
        {
            Id = id;
            Loop = -1;
            AnimationMode = -1;
            Cycles = 99;
            ReplayMode = 2;
            ABoolean413 = false;
            ABoolean409 = false;
            WalkingProperties = -1;
            Priority = 5;
            IsTweened = false;
            WeaponDisplayedItemId = -1;
            ShieldDisplayedItemId = -1;
        }

        /// <summary>
        /// Set's some information about animation.
        /// </summary>
        public void AfterDecode()
        {
            if (AnimationMode == -1)
            {
                if (ABooleanArray414 != null)
                    AnimationMode = 2;
                else
                    AnimationMode = 0;
            }
            if (WalkingProperties == -1)
            {
                if (ABooleanArray414 != null)
                    WalkingProperties = 2;
                else
                    WalkingProperties = 0;
            }
        }

        /// <summary>
        /// Estimated duration in server ticks.
        /// </summary>
        /// <returns></returns>
        public int EstimatedDurationInServerTicks() => (EstimatedDurationInClientTicks() * 30) / 1000;

        /// <summary>
        /// Get's estimated duration in client ticks.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int EstimatedDurationInClientTicks()
        {
            if (Delays == null)
                return -1;

            return Delays.Sum();
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
                if (opcode == 1)
                {
                    int i2 = buffer.ReadUnsignedShort();
                    Delays = new int[i2];
                    for (int i3 = 0; i3 < i2; i3++)
                        Delays[i3] = buffer.ReadUnsignedShort();
                    Frames = new int[i2];
                    for (int i4 = 0; i4 < i2; i4++)
                        Frames[i4] = buffer.ReadUnsignedShort();
                    for (int i5 = 0; i2 > i5; i5++)
                        Frames[i5] = (buffer.ReadUnsignedShort() << 16) + Frames[i5];
                }
                else if (opcode == 2)
                    Loop = buffer.ReadUnsignedShort();
                else if (opcode == 3)
                {
                    ABooleanArray414 = new bool[256];
                    int i6 = buffer.ReadUnsignedByte();
                    for (int i7 = 0; i6 > i7; i7++)
                        ABooleanArray414[buffer.ReadUnsignedByte()] = true;
                }
                else if (opcode == 5)
                    Priority = buffer.ReadUnsignedByte();
                else if (opcode != 6)
                {
                    if (opcode == 7)
                        WeaponDisplayedItemId = buffer.ReadUnsignedShort();
                    else if (opcode != 8)
                    {
                        if (opcode != 9)
                        {
                            if (opcode != 10)
                            {
                                if (opcode != 11)
                                {
                                    if (opcode == 12)
                                    {
                                        int i8 = buffer.ReadUnsignedByte();
                                        InterfaceFrames = new int[i8];
                                        for (int i9 = 0; i9 < i8; i9++)
                                            InterfaceFrames[i9] = buffer.ReadUnsignedShort();
                                        for (int i10 = 0; i8 > i10; i10++)
                                            InterfaceFrames[i10] = (buffer.ReadUnsignedShort() << 16) + InterfaceFrames[i10];
                                    }
                                    else if (opcode == 13)
                                    {
                                        int i11 = buffer.ReadUnsignedShort();
                                        SoundSettings = new int[i11][];
                                        for (int i12 = 0; i12 < i11; i12++)
                                        {
                                            int i13 = buffer.ReadUnsignedByte();
                                            if (i13 > 0)
                                            {
                                                SoundSettings[i12] = new int[i13];
                                                SoundSettings[i12][0] = buffer.ReadMedInt(); // sound hash 		
                                                                                             //int soundID = soundHash >> 8;
                                                                                             //int timesToPlay = soundHash >> 5 & 0x7;
                                                                                             //int i_7_ = soundHash & 0x1f;
                                                for (int i14 = 1; i13 > i14; i14++)
                                                    SoundSettings[i12][i14] = buffer.ReadUnsignedShort();
                                            }
                                        }
                                    }
                                    else if (opcode != 14)
                                    {
                                        if (opcode != 15)
                                        {
                                            if (opcode != 16)
                                            {
                                                if (opcode == 18)
                                                    ABoolean409 = true;
                                                else if (opcode == 19)
                                                {
                                                    if (SoundVolumes == null)
                                                    {
                                                        SoundVolumes = new int[SoundSettings!.Length];
                                                        for (int i15 = 0; i15 < SoundSettings!.Length; i15++)
                                                            SoundVolumes[i15] = 255;
                                                    }
                                                    SoundVolumes[buffer.ReadUnsignedByte()] = buffer.ReadUnsignedByte();
                                                }
                                                else if (opcode == 20)
                                                {
                                                    if (MinSoundSpeed == null || MaxSoundSpeed == null)
                                                    {
                                                        MinSoundSpeed = (new int[SoundSettings.Length]);
                                                        MaxSoundSpeed = (new int[SoundSettings.Length]);
                                                        for (int i16 = 0; (SoundSettings.Length > i16); i16++)
                                                        {
                                                            MinSoundSpeed[i16] = 256;
                                                            MaxSoundSpeed[i16] = 256;
                                                        }
                                                    }
                                                    int i17 = buffer.ReadUnsignedByte();
                                                    MinSoundSpeed[i17] = buffer.ReadUnsignedShort();
                                                    MaxSoundSpeed[i17] = buffer.ReadUnsignedShort();
                                                }
                                            }
                                            else if (opcode == 22)
                                            {
                                                buffer.ReadUnsignedByte(); // sound repeats
                                            }
                                            else if (opcode == 249)
                                            {
                                                int length = buffer.ReadUnsignedByte();
                                                ExtraData = new Dictionary<int, object>(length);
                                                for (int i = 0; i < length; i++)
                                                {
                                                    bool readString = buffer.ReadUnsignedByte() == 1;
                                                    int key = buffer.ReadMedInt();
                                                    if (readString)
                                                        ExtraData.Add(key, buffer.ReadString());
                                                    else
                                                        ExtraData.Add(key, buffer.ReadInt());
                                                }
                                            }
                                        }
                                        else
                                            IsTweened = true;
                                    }
                                    else
                                        ABoolean413 = true;
                                }
                                else
                                    ReplayMode = buffer.ReadUnsignedByte();
                            }
                            else
                                WalkingProperties = buffer.ReadUnsignedByte();
                        }
                        else
                            AnimationMode = buffer.ReadUnsignedByte();
                    }
                    else
                        Cycles = buffer.ReadUnsignedByte();
                }
                else
                    ShieldDisplayedItemId = buffer.ReadUnsignedShort();
            }
        }
        public MemoryStream Encode() => throw new System.NotImplementedException();
    }
}
