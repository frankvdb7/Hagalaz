using System;
using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    public class AnimationTypeCodec : IAnimationTypeCodec
    {
        public IAnimationType Decode(int id, MemoryStream stream)
        {
            var animationType = new AnimationType(id);
            Decode(animationType, stream);
            return animationType;
        }

        private void Decode(AnimationType animationType, MemoryStream buffer)
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
                    animationType.Delays = new int[i2];
                    for (int i3 = 0; i3 < i2; i3++)
                        animationType.Delays[i3] = buffer.ReadUnsignedShort();
                    animationType.Frames = new int[i2];
                    for (int i4 = 0; i4 < i2; i4++)
                        animationType.Frames[i4] = buffer.ReadUnsignedShort();
                    for (int i5 = 0; i2 > i5; i5++)
                        animationType.Frames[i5] = (buffer.ReadUnsignedShort() << 16) + animationType.Frames[i5];
                }
                else if (opcode == 2)
                    animationType.Loop = buffer.ReadUnsignedShort();
                else if (opcode == 3)
                {
                    animationType.ABooleanArray414 = new bool[256];
                    int i6 = buffer.ReadUnsignedByte();
                    for (int i7 = 0; i6 > i7; i7++)
                        animationType.ABooleanArray414[buffer.ReadUnsignedByte()] = true;
                }
                else if (opcode == 5)
                    animationType.Priority = buffer.ReadUnsignedByte();
                else if (opcode != 6)
                {
                    if (opcode == 7)
                        animationType.WeaponDisplayedItemId = buffer.ReadUnsignedShort();
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
                                        animationType.InterfaceFrames = new int[i8];
                                        for (int i9 = 0; i9 < i8; i9++)
                                            animationType.InterfaceFrames[i9] = buffer.ReadUnsignedShort();
                                        for (int i10 = 0; i8 > i10; i10++)
                                            animationType.InterfaceFrames[i10] = (buffer.ReadUnsignedShort() << 16) + animationType.InterfaceFrames[i10];
                                    }
                                    else if (opcode == 13)
                                    {
                                        int i11 = buffer.ReadUnsignedShort();
                                        animationType.SoundSettings = new int[i11][];
                                        for (int i12 = 0; i12 < i11; i12++)
                                        {
                                            int i13 = buffer.ReadUnsignedByte();
                                            if (i13 > 0)
                                            {
                                                animationType.SoundSettings[i12] = new int[i13];
                                                animationType.SoundSettings[i12][0] = buffer.ReadMedInt();
                                                for (int i14 = 1; i13 > i14; i14++)
                                                    animationType.SoundSettings[i12][i14] = buffer.ReadUnsignedShort();
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
                                                    animationType.ABoolean409 = true;
                                                else if (opcode == 19)
                                                {
                                                    if (animationType.SoundVolumes == null)
                                                    {
                                                        animationType.SoundVolumes = new int[animationType.SoundSettings!.Length];
                                                        for (int i15 = 0; i15 < animationType.SoundSettings!.Length; i15++)
                                                            animationType.SoundVolumes[i15] = 255;
                                                    }
                                                    animationType.SoundVolumes[buffer.ReadUnsignedByte()] = buffer.ReadUnsignedByte();
                                                }
                                                else if (opcode == 20)
                                                {
                                                    if (animationType.MinSoundSpeed == null || animationType.MaxSoundSpeed == null)
                                                    {
                                                        animationType.MinSoundSpeed = (new int[animationType.SoundSettings!.Length]);
                                                        animationType.MaxSoundSpeed = (new int[animationType.SoundSettings!.Length]);
                                                        for (int i16 = 0; (animationType.SoundSettings!.Length > i16); i16++)
                                                        {
                                                            animationType.MinSoundSpeed[i16] = 256;
                                                            animationType.MaxSoundSpeed[i16] = 256;
                                                        }
                                                    }
                                                    int i17 = buffer.ReadUnsignedByte();
                                                    animationType.MinSoundSpeed[i17] = buffer.ReadUnsignedShort();
                                                    animationType.MaxSoundSpeed[i17] = buffer.ReadUnsignedShort();
                                                }
                                            }
                                            else if (opcode == 22)
                                            {
                                                buffer.ReadUnsignedByte();
                                            }
                                            else if (opcode == 249)
                                            {
                                                int length = buffer.ReadUnsignedByte();
                                                animationType.ExtraData = new Dictionary<int, object>(length);
                                                for (int i = 0; i < length; i++)
                                                {
                                                    bool readString = buffer.ReadUnsignedByte() == 1;
                                                    int key = buffer.ReadMedInt();
                                                    if (readString)
                                                        animationType.ExtraData.Add(key, buffer.ReadString());
                                                    else
                                                        animationType.ExtraData.Add(key, buffer.ReadInt());
                                                }
                                            }
                                        }
                                        else
                                            animationType.IsTweened = true;
                                    }
                                    else
                                        animationType.ABoolean413 = true;
                                }
                                else
                                    animationType.ReplayMode = buffer.ReadUnsignedByte();
                            }
                            else
                                animationType.WalkingProperties = buffer.ReadUnsignedByte();
                        }
                        else
                            animationType.AnimationMode = buffer.ReadUnsignedByte();
                    }
                    else
                        animationType.Cycles = buffer.ReadUnsignedByte();
                }
                else
                    animationType.ShieldDisplayedItemId = buffer.ReadUnsignedShort();
            }
        }

        public MemoryStream Encode(IAnimationType instance) => throw new NotImplementedException();
    }
}