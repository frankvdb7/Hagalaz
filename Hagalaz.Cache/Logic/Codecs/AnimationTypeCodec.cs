using System;
using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs
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
                    int count = buffer.ReadUnsignedShort();
                    animationType.Delays = new int[count];
                    for (int i = 0; i < count; i++)
                        animationType.Delays[i] = buffer.ReadUnsignedShort();

                    animationType.Frames = new int[count];
                    for (int i = 0; i < count; i++)
                        animationType.Frames[i] = buffer.ReadUnsignedShort();
                    for (int i = 0; i < count; i++)
                        animationType.Frames[i] = (buffer.ReadUnsignedShort() << 16) + animationType.Frames[i];
                }
                else if (opcode == 2)
                {
                    animationType.Loop = buffer.ReadUnsignedShort();
                }
                else if (opcode == 3)
                {
                    animationType.ABooleanArray414 = new bool[256];
                    int count = buffer.ReadUnsignedByte();
                    for (int i = 0; i < count; i++)
                        animationType.ABooleanArray414[buffer.ReadUnsignedByte()] = true;
                }
                else if (opcode == 5)
                {
                    animationType.Priority = buffer.ReadUnsignedByte();
                }
                else if (opcode == 6)
                {
                    animationType.ShieldDisplayedItemId = buffer.ReadUnsignedShort();
                }
                else if (opcode == 7)
                {
                    animationType.WeaponDisplayedItemId = buffer.ReadUnsignedShort();
                }
                else if (opcode == 8)
                {
                    animationType.Cycles = buffer.ReadUnsignedByte();
                }
                else if (opcode == 9)
                {
                    animationType.AnimationMode = buffer.ReadUnsignedByte();
                }
                else if (opcode == 10)
                {
                    animationType.WalkingProperties = buffer.ReadUnsignedByte();
                }
                else if (opcode == 11)
                {
                    animationType.ReplayMode = buffer.ReadUnsignedByte();
                }
                else if (opcode == 12)
                {
                    int count = buffer.ReadUnsignedByte();
                    animationType.InterfaceFrames = new int[count];
                    for (int i = 0; i < count; i++)
                        animationType.InterfaceFrames[i] = buffer.ReadUnsignedShort();
                    for (int i = 0; i < count; i++)
                        animationType.InterfaceFrames[i] = (buffer.ReadUnsignedShort() << 16) + animationType.InterfaceFrames[i];
                }
                else if (opcode == 13)
                {
                    int count = buffer.ReadUnsignedShort();
                    animationType.SoundSettings = new int[count][];
                    for (int i = 0; i < count; i++)
                    {
                        int innerCount = buffer.ReadUnsignedByte();
                        if (innerCount > 0)
                        {
                            animationType.SoundSettings[i] = new int[innerCount];
                            animationType.SoundSettings[i][0] = buffer.ReadMedInt();
                            for (int j = 1; j < innerCount; j++)
                                animationType.SoundSettings[i][j] = buffer.ReadUnsignedShort();
                        }
                    }
                }
                else if (opcode == 14)
                {
                    animationType.ABoolean413 = true;
                }
                else if (opcode == 15)
                {
                    animationType.IsTweened = true;
                }
                else if (opcode == 16)
                {
                    // Opcode 16 is ignored in the original implementation.
                }
                else if (opcode == 18)
                {
                    animationType.ABoolean409 = true;
                }
                else if (opcode == 19)
                {
                    if (animationType.SoundSettings == null)
                        throw new InvalidDataException("AnimationType: opcode 19 appeared before opcode 13.");
                    if (animationType.SoundVolumes == null)
                    {
                        animationType.SoundVolumes = new int[animationType.SoundSettings.Length];
                        for (int i = 0; i < animationType.SoundSettings.Length; i++)
                            animationType.SoundVolumes[i] = 255;
                    }
                    animationType.SoundVolumes[buffer.ReadUnsignedByte()] = buffer.ReadUnsignedByte();
                }
                else if (opcode == 20)
                {
                    if (animationType.SoundSettings == null)
                        throw new InvalidDataException("AnimationType: opcode 20 appeared before opcode 13.");

                    if (animationType.MinSoundSpeed == null || animationType.MaxSoundSpeed == null)
                    {
                        animationType.MinSoundSpeed = new int[animationType.SoundSettings.Length];
                        animationType.MaxSoundSpeed = new int[animationType.SoundSettings.Length];
                        for (int i = 0; i < animationType.SoundSettings.Length; i++)
                        {
                            animationType.MinSoundSpeed[i] = 256;
                            animationType.MaxSoundSpeed[i] = 256;
                        }
                    }
                    int index = buffer.ReadUnsignedByte();
                    animationType.MinSoundSpeed[index] = buffer.ReadUnsignedShort();
                    animationType.MaxSoundSpeed[index] = buffer.ReadUnsignedShort();
                }
                else if (opcode == 22)
                {
                    buffer.ReadUnsignedByte();
                }
                else if (opcode == 249)
                {
                    int length = buffer.ReadUnsignedByte();
                    if (animationType.ExtraData == null)
                    {
                        animationType.ExtraData = new Dictionary<int, object>(length);
                    }
                    for (int i = 0; i < length; i++)
                    {
                        bool isString = buffer.ReadUnsignedByte() == 1;
                        int key = buffer.ReadMedInt();
                        object value = isString ? buffer.ReadString() : buffer.ReadInt();
                        animationType.ExtraData[key] = value;
                    }
                }
            }
        }

        public MemoryStream Encode(IAnimationType instance) => throw new NotImplementedException();
    }
}