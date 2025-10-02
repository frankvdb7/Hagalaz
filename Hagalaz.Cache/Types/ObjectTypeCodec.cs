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

            if (obj.ModelIDs != null && obj.Shapes != null && obj.ModelIDs.Length > 0 && obj.ModelIDs.Length == obj.Shapes.Length)
            {
                writer.WriteByte(ObjectTypeOpcodes.ModelIDs);
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
                writer.WriteByte(ObjectTypeOpcodes.Name);
                writer.WriteString(obj.Name);
            }

            if (obj.SizeX != 1)
            {
                writer.WriteByte(ObjectTypeOpcodes.SizeX);
                writer.WriteByte((byte)obj.SizeX);
            }

            if (obj.SizeY != 1)
            {
                writer.WriteByte(ObjectTypeOpcodes.SizeY);
                writer.WriteByte((byte)obj.SizeY);
            }

            if (obj.ClipType == 0)
            {
                writer.WriteByte(ObjectTypeOpcodes.ClipTypeSolidFalse);
            }
            else if (!obj.Solid)
            {
                writer.WriteByte(ObjectTypeOpcodes.SolidFalse);
            }

            if (obj.Interactable != -1)
            {
                writer.WriteByte(ObjectTypeOpcodes.Interactable);
                writer.WriteByte((byte)obj.Interactable);
            }

            if (obj.GroundContoured == 1)
            {
                writer.WriteByte(ObjectTypeOpcodes.GroundContoured1);
            }

            if (obj.DelayShading)
            {
                writer.WriteByte(ObjectTypeOpcodes.DelayShading);
            }

            if (obj.Occludes == 1)
            {
                writer.WriteByte(ObjectTypeOpcodes.Occludes1);
            }

            if (obj.AnimationIDs != null && obj.AnimationIDs.Length == 1)
            {
                writer.WriteByte(ObjectTypeOpcodes.AnimationID);
                writer.WriteBigSmart(obj.AnimationIDs[0]);
            }

            if (obj.ClipType == 1)
            {
                writer.WriteByte(ObjectTypeOpcodes.ClipType1);
            }

            if (obj.DecorDisplacement != 64)
            {
                writer.WriteByte(ObjectTypeOpcodes.DecorDisplacement);
                writer.WriteByte((byte)(obj.DecorDisplacement >> 2));
            }

            if (obj.Ambient != 0)
            {
                writer.WriteByte(ObjectTypeOpcodes.Ambient);
                writer.WriteSignedByte((sbyte)obj.Ambient);
            }

            if (obj.Contrast != 0)
            {
                writer.WriteByte(ObjectTypeOpcodes.Contrast);
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

            if (obj.OriginalColors != null && obj.ModifiedColors != null && obj.OriginalColors.Length == obj.ModifiedColors.Length)
            {
                writer.WriteByte(ObjectTypeOpcodes.Colors);
                writer.WriteByte((byte)obj.OriginalColors.Length);
                for(int i = 0; i < obj.OriginalColors.Length; i++)
                {
                    writer.WriteShort(obj.OriginalColors[i]);
                    writer.WriteShort(obj.ModifiedColors[i]);
                }
            }

            if (obj.AShortArray831 != null && obj.AShortArray762 != null && obj.AShortArray831.Length == obj.AShortArray762.Length)
            {
                writer.WriteByte(ObjectTypeOpcodes.ShortArrays);
                writer.WriteByte((byte)obj.AShortArray831.Length);
                for(int i = 0; i < obj.AShortArray831.Length; i++)
                {
                    writer.WriteShort(obj.AShortArray831[i]);
                    writer.WriteShort(obj.AShortArray762[i]);
                }
            }

            if (obj.AByteArray816 != null)
            {
                writer.WriteByte(ObjectTypeOpcodes.ByteArray816);
                writer.WriteByte((byte)obj.AByteArray816.Length);
                foreach (var val in obj.AByteArray816)
                {
                    writer.WriteSignedByte(val);
                }
            }

            if(obj.Inverted)
            {
                writer.WriteByte(ObjectTypeOpcodes.Inverted);
            }

            if (!obj.CastsShadow)
            {
                writer.WriteByte(ObjectTypeOpcodes.CastsShadowFalse);
            }

            if (obj.ScaleX != 128)
            {
                writer.WriteByte(ObjectTypeOpcodes.ScaleX);
                writer.WriteShort(obj.ScaleX);
            }

            if (obj.ScaleY != 128)
            {
                writer.WriteByte(ObjectTypeOpcodes.ScaleY);
                writer.WriteShort(obj.ScaleY);
            }

            if (obj.ScaleZ != 128)
            {
                writer.WriteByte(ObjectTypeOpcodes.ScaleZ);
                writer.WriteShort(obj.ScaleZ);
            }

            if (obj.Surroundings != 0)
            {
                writer.WriteByte(ObjectTypeOpcodes.Surroundings);
                writer.WriteByte(obj.Surroundings);
            }

            if (obj.OffsetX != 0)
            {
                writer.WriteByte(ObjectTypeOpcodes.OffsetX);
                writer.WriteShort(obj.OffsetX >> 2);
            }

            if (obj.OffsetY != 0)
            {
                writer.WriteByte(ObjectTypeOpcodes.OffsetY);
                writer.WriteShort(obj.OffsetY >> 2);
            }

            if (obj.OffsetZ != 0)
            {
                writer.WriteByte(ObjectTypeOpcodes.OffsetZ);
                writer.WriteShort(obj.OffsetZ >> 2);
            }

            if (obj.ObstructsGround)
            {
                writer.WriteByte(ObjectTypeOpcodes.ObstructsGround);
            }

            if (obj.Gateway)
            {
                writer.WriteByte(ObjectTypeOpcodes.Gateway);
            }

            if (obj.SupportItemsFlag != -1)
            {
                writer.WriteByte(ObjectTypeOpcodes.SupportItemsFlag);
                writer.WriteByte((byte)obj.SupportItemsFlag);
            }

            if (obj.AmbientSoundID != -1)
            {
                writer.WriteByte(ObjectTypeOpcodes.AmbientSound);
                writer.WriteShort(obj.AmbientSoundID);
                writer.WriteByte((byte)obj.AmbientSoundHearDistance);
            }

            if (obj.AudioTracks != null)
            {
                writer.WriteByte(ObjectTypeOpcodes.AudioTracks);
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
                writer.WriteByte(ObjectTypeOpcodes.GroundContoured2);
                writer.WriteByte((byte)(obj.AnInt780 / 256));
            }

            if (obj.Hidden)
            {
                writer.WriteByte(ObjectTypeOpcodes.Hidden);
            }

            if (!obj.ABoolean779)
            {
                writer.WriteByte(ObjectTypeOpcodes.ABoolean779False);
            }

            if (!obj.ABoolean838)
            {
                writer.WriteByte(ObjectTypeOpcodes.ABoolean838False);
            }

            if (obj.MembersOnly)
            {
                writer.WriteByte(ObjectTypeOpcodes.MembersOnly);
            }

            if (obj.TransformToIDs != null)
            {
                bool hasExtra = obj.TransformToIDs[obj.TransformToIDs.Length - 1] != -1;
                writer.WriteByte((byte)(hasExtra ? ObjectTypeOpcodes.VarpTransformWithExtra : ObjectTypeOpcodes.VarpTransform));
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
                writer.WriteByte(ObjectTypeOpcodes.GroundContoured3Short);
                writer.WriteShort(obj.AnInt780);
            }

            if(obj.GroundContoured == 4)
            {
                writer.WriteByte(ObjectTypeOpcodes.GroundContoured4);
            }

            if(obj.GroundContoured == 5 && obj.AnInt780 != -1)
            {
                writer.WriteByte(ObjectTypeOpcodes.GroundContoured5);
                writer.WriteShort(obj.AnInt780);
            }

            if (obj.AdjustMapSceneRotation)
            {
                writer.WriteByte(ObjectTypeOpcodes.AdjustMapSceneRotation);
            }

            if (obj.HasAnimation)
            {
                writer.WriteByte(ObjectTypeOpcodes.HasAnimation);
            }

            if(obj.AnInt788 != -1)
            {
                writer.WriteByte(ObjectTypeOpcodes.AnInt788);
                writer.WriteByte((byte)obj.AnInt788);
                writer.WriteShort(obj.AnInt827);
            }

            if(obj.AnInt764 != -1)
            {
                writer.WriteByte(ObjectTypeOpcodes.AnInt764);
                writer.WriteByte((byte)obj.AnInt764);
                writer.WriteShort(obj.AnInt828);
            }

            if (obj.MapSpriteRotation != 0)
            {
                writer.WriteByte(ObjectTypeOpcodes.MapSpriteRotation);
                writer.WriteByte((byte)obj.MapSpriteRotation);
            }

            if (obj.MapSpriteType != -1)
            {
                writer.WriteByte(ObjectTypeOpcodes.MapSpriteType);
                writer.WriteShort(obj.MapSpriteType);
            }

            if (obj.Occludes == 0)
            {
                writer.WriteByte(ObjectTypeOpcodes.Occludes0);
            }

            if (obj.AmbientSoundVolume != 255)
            {
                writer.WriteByte(ObjectTypeOpcodes.AmbientSoundVolume);
                writer.WriteByte((byte)obj.AmbientSoundVolume);
            }

            if(obj.FlipMapSprite)
            {
                writer.WriteByte(ObjectTypeOpcodes.FlipMapSprite);
            }

            if (obj.AnimationIDs != null && obj.AnIntArray784 != null && obj.AnimationIDs.Length == obj.AnIntArray784.Length)
            {
                writer.WriteByte(ObjectTypeOpcodes.AnimationIDs);
                writer.WriteByte((byte)obj.AnimationIDs.Length);
                for (int i = 0; i < obj.AnimationIDs.Length; i++)
                {
                    writer.WriteBigSmart(obj.AnimationIDs[i] == -1 ? 65535 : obj.AnimationIDs[i]);
                    writer.WriteByte((byte)obj.AnIntArray784[i]);
                }
            }

            if (obj.MapIcon != -1)
            {
                writer.WriteByte(ObjectTypeOpcodes.MapIcon);
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
                writer.WriteByte(ObjectTypeOpcodes.QuestIDs);
                writer.WriteByte((byte)obj.QuestIDs.Length);
                foreach (var questId in obj.QuestIDs)
                {
                    writer.WriteShort(questId);
                }
            }

            if (obj.GroundContoured == 3 && obj.AnInt780 != -1)
            {
                writer.WriteByte(ObjectTypeOpcodes.GroundContoured3Int);
                writer.WriteInt(obj.AnInt780);
            }

            if (obj.AByte826 != 0 || obj.AByte790 != 0 || obj.AByte821 != 0 || obj.AByte787 != 0)
            {
                writer.WriteByte(ObjectTypeOpcodes.Bytes);
                writer.WriteSignedByte(obj.AByte826);
                writer.WriteSignedByte(obj.AByte790);
                writer.WriteSignedByte(obj.AByte821);
                writer.WriteSignedByte(obj.AByte787);
            }

            if (obj.AnInt782 != 0)
            {
                writer.WriteByte(ObjectTypeOpcodes.AnInt782);
                writer.WriteShort(obj.AnInt782);
            }

            if (obj.AnInt830 != 0)
            {
                writer.WriteByte(ObjectTypeOpcodes.AnInt830);
                writer.WriteShort(obj.AnInt830);
            }

            if (obj.AnInt778 != 0)
            {
                writer.WriteByte(ObjectTypeOpcodes.AnInt778);
                writer.WriteShort(obj.AnInt778);
            }

            if (obj.AnInt776 != 0)
            {
                writer.WriteByte(ObjectTypeOpcodes.AnInt776);
                writer.WriteShort(obj.AnInt776);
            }

            if (obj.ABoolean810)
            {
                writer.WriteByte(ObjectTypeOpcodes.ABoolean810);
            }

            if (obj.ABoolean781)
            {
                writer.WriteByte(ObjectTypeOpcodes.ABoolean781);
            }

            if (obj.AnInt823 != 960)
            {
                writer.WriteByte(ObjectTypeOpcodes.AnInt823);
                writer.WriteSmart(obj.AnInt823);
            }

            if (obj.AnInt773 != 0)
            {
                writer.WriteByte(ObjectTypeOpcodes.AnInt773);
                writer.WriteSmart(obj.AnInt773);
            }

            if (obj.AnInt825 != 256 || obj.AnInt808 != 256)
            {
                writer.WriteByte(ObjectTypeOpcodes.AnInts825And808);
                writer.WriteShort(obj.AnInt825);
                writer.WriteShort(obj.AnInt808);
            }

            if (obj.ABoolean835)
            {
                writer.WriteByte(ObjectTypeOpcodes.ABoolean835);
            }

            if (obj.AnInt813 != 0)
            {
                writer.WriteByte(ObjectTypeOpcodes.AnInt813);
                writer.WriteByte((byte)obj.AnInt813);
            }

            if (obj.ExtraData != null && obj.ExtraData.Count > 0)
            {
                writer.WriteByte(ObjectTypeOpcodes.ExtraData);
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
                if (opcode != ObjectTypeOpcodes.ModelIDs)
                {
                    if (opcode != ObjectTypeOpcodes.Name)
                    {
                        if (opcode != ObjectTypeOpcodes.SizeX)
                        {
                            if (opcode == ObjectTypeOpcodes.SizeY)
                                objectType.SizeY = buffer.ReadUnsignedByte();
                            else if (opcode != ObjectTypeOpcodes.ClipTypeSolidFalse)
                            {
                                if (opcode == ObjectTypeOpcodes.SolidFalse)
                                    objectType.Solid = false;
                                else if (opcode != ObjectTypeOpcodes.Interactable)
                                {
                                    if (opcode == ObjectTypeOpcodes.GroundContoured1)
                                        objectType.GroundContoured = 1;
                                    else if (opcode == ObjectTypeOpcodes.DelayShading)
                                        objectType.DelayShading = true;
                                    else if (opcode != ObjectTypeOpcodes.Occludes1)
                                    {
                                        if (opcode == ObjectTypeOpcodes.AnimationID)
                                        {
                                            int animationID = buffer.ReadBigSmart();
                                            if (animationID != 65535)
                                                objectType.AnimationIDs = new int[] { animationID };
                                        }
                                        else if (opcode != ObjectTypeOpcodes.ClipType1)
                                        {
                                            if (opcode != ObjectTypeOpcodes.DecorDisplacement)
                                            {
                                                if (opcode != ObjectTypeOpcodes.Ambient)
                                                {
                                                    if (opcode != ObjectTypeOpcodes.Contrast)
                                                    {
                                                        if (opcode >= 30 && opcode < 35)
                                                            objectType.Actions[opcode - 30] = (buffer.ReadString());
                                                        else if (opcode != ObjectTypeOpcodes.Colors)
                                                        {
                                                            if (opcode == ObjectTypeOpcodes.ShortArrays)
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
                                                            else if (opcode != ObjectTypeOpcodes.ByteArray816)
                                                            {
                                                                if (opcode == 44)
                                                                {
                                                                    int i16 = buffer.ReadUnsignedShort();
                                                                }
                                                                else if (opcode == 45)
                                                                {
                                                                    int i21 = buffer.ReadUnsignedShort();
                                                                }
                                                                else if (opcode == ObjectTypeOpcodes.Inverted)
                                                                    objectType.Inverted = true;
                                                                else if (opcode == ObjectTypeOpcodes.CastsShadowFalse)
                                                                    objectType.CastsShadow = false;
                                                                else if (opcode != ObjectTypeOpcodes.ScaleX)
                                                                {
                                                                    if (opcode == ObjectTypeOpcodes.ScaleY)
                                                                        objectType.ScaleY = (buffer.ReadUnsignedShort());
                                                                    else if (opcode == ObjectTypeOpcodes.ScaleZ)
                                                                        objectType.ScaleZ = (buffer.ReadUnsignedShort());
                                                                    else if (opcode != ObjectTypeOpcodes.Surroundings)
                                                                    {
                                                                        if (opcode == ObjectTypeOpcodes.OffsetX)
                                                                            objectType.OffsetX = ((buffer.ReadShort()) << 2);
                                                                        else if (opcode != ObjectTypeOpcodes.OffsetY)
                                                                        {
                                                                            if (opcode != ObjectTypeOpcodes.OffsetZ)
                                                                            {
                                                                                if (opcode != ObjectTypeOpcodes.ObstructsGround)
                                                                                {
                                                                                    if (opcode != ObjectTypeOpcodes.Gateway)
                                                                                    {
                                                                                        if (opcode != ObjectTypeOpcodes.SupportItemsFlag)
                                                                                        {
                                                                                            if (opcode != ObjectTypeOpcodes.VarpTransform && opcode != ObjectTypeOpcodes.VarpTransformWithExtra)
                                                                                            {
                                                                                                if (opcode != ObjectTypeOpcodes.AmbientSound)
                                                                                                {
                                                                                                    if (opcode != ObjectTypeOpcodes.AudioTracks)
                                                                                                    {
                                                                                                        if (opcode != ObjectTypeOpcodes.GroundContoured2)
                                                                                                        {
                                                                                                            if (opcode != ObjectTypeOpcodes.Hidden)
                                                                                                            {
                                                                                                                if (opcode == ObjectTypeOpcodes.ABoolean779False)
                                                                                                                    objectType.ABoolean779 = false;
                                                                                                                else if (opcode != ObjectTypeOpcodes.ABoolean838False)
                                                                                                                {
                                                                                                                    if (opcode != ObjectTypeOpcodes.MembersOnly)
                                                                                                                    {
                                                                                                                        if (opcode == ObjectTypeOpcodes.GroundContoured3Short)
                                                                                                                        {
                                                                                                                            objectType.GroundContoured = (sbyte)3;
                                                                                                                            objectType.AnInt780 = buffer.ReadUnsignedShort();
                                                                                                                        }
                                                                                                                        else if (opcode == ObjectTypeOpcodes.GroundContoured4)
                                                                                                                            objectType.GroundContoured = (sbyte)4;
                                                                                                                        else if (opcode != ObjectTypeOpcodes.GroundContoured5)
                                                                                                                        {
                                                                                                                            if (opcode != ObjectTypeOpcodes.AdjustMapSceneRotation)
                                                                                                                            {
                                                                                                                                if (opcode == ObjectTypeOpcodes.HasAnimation)
                                                                                                                                    objectType.HasAnimation = true;
                                                                                                                                else if (opcode != ObjectTypeOpcodes.AnInt788)
                                                                                                                                {
                                                                                                                                    if (opcode == ObjectTypeOpcodes.AnInt764)
                                                                                                                                    {
                                                                                                                                        objectType.AnInt764 = buffer.ReadUnsignedByte();
                                                                                                                                        objectType.AnInt828 = buffer.ReadUnsignedShort();
                                                                                                                                    }
                                                                                                                                    else if (opcode != ObjectTypeOpcodes.MapSpriteRotation)
                                                                                                                                    {
                                                                                                                                        if (opcode == ObjectTypeOpcodes.MapSpriteType)
                                                                                                                                            objectType.MapSpriteType = buffer.ReadUnsignedShort();
                                                                                                                                        else if (opcode != ObjectTypeOpcodes.Occludes0)
                                                                                                                                        {
                                                                                                                                            if (opcode == ObjectTypeOpcodes.AmbientSoundVolume)
                                                                                                                                                objectType.AmbientSoundVolume = buffer.ReadUnsignedByte();
                                                                                                                                            else if (opcode == ObjectTypeOpcodes.FlipMapSprite)
                                                                                                                                                objectType.FlipMapSprite = true;
                                                                                                                                            else if (opcode == ObjectTypeOpcodes.AnimationIDs)
                                                                                                                                            {
                                                                                                                                                int count = buffer.ReadUnsignedByte();
                                                                                                                                                objectType.AnIntArray784 = new int[count];
                                                                                                                                                objectType.AnimationIDs = new int[count];
                                                                                                                                                int total = 0;
                                                                                                                                                for (int i = 0; i < count; i++)
                                                                                                                                                {
                                                                                                                                                    objectType.AnimationIDs[i] = buffer.ReadBigSmart();
                                                                                                                                                    if (objectType.AnimationIDs[i] == 65535)
                                                                                                                                                    {
                                                                                                                                                        objectType.AnimationIDs[i] = -1;
                                                                                                                                                    }
                                                                                                                                                    total += objectType.AnIntArray784[i] = buffer.ReadUnsignedByte();
                                                                                                                                                }
                                                                                                                                                if (total > 0)
                                                                                                                                                {
                                                                                                                                                    for (int i = 0; i < count; i++)
                                                                                                                                                    {
                                                                                                                                                        objectType.AnIntArray784[i] = objectType.AnIntArray784[i] * 65535 / total;
                                                                                                                                                    }
                                                                                                                                                }
                                                                                                                                            }
                                                                                                                                            else if (opcode != ObjectTypeOpcodes.MapIcon)
                                                                                                                                            {
                                                                                                                                                if (opcode < 150 || opcode >= 155)
                                                                                                                                                {
                                                                                                                                                    if (opcode == ObjectTypeOpcodes.QuestIDs)
                                                                                                                                                    {
                                                                                                                                                        int i26 = buffer.ReadUnsignedByte();
                                                                                                                                                        objectType.QuestIDs = new int[i26];
                                                                                                                                                        for (int i27 = 0; i27 < i26; i27++)
                                                                                                                                                            objectType.QuestIDs[i27] = buffer.ReadUnsignedShort();
                                                                                                                                                    }
                                                                                                                                                    else if (opcode != ObjectTypeOpcodes.GroundContoured3Int)
                                                                                                                                                    {
                                                                                                                                                        if (opcode == ObjectTypeOpcodes.Bytes)
                                                                                                                                                        {
                                                                                                                                                            objectType.AByte826 = (sbyte)buffer.ReadSignedByte();
                                                                                                                                                            objectType.AByte790 = (sbyte)buffer.ReadSignedByte();
                                                                                                                                                            objectType.AByte821 = (sbyte)buffer.ReadSignedByte();
                                                                                                                                                            objectType.AByte787 = (sbyte)buffer.ReadSignedByte();
                                                                                                                                                        }
                                                                                                                                                        else if (opcode != ObjectTypeOpcodes.AnInt782)
                                                                                                                                                        {
                                                                                                                                                            if (opcode == ObjectTypeOpcodes.AnInt830)
                                                                                                                                                                objectType.AnInt830 = buffer.ReadShort();
                                                                                                                                                            else if (opcode == ObjectTypeOpcodes.AnInt778)
                                                                                                                                                                objectType.AnInt778 = buffer.ReadShort();
                                                                                                                                                            else if (opcode == ObjectTypeOpcodes.AnInt776)
                                                                                                                                                                objectType.AnInt776 = buffer.ReadUnsignedShort();
                                                                                                                                                            else if (opcode == ObjectTypeOpcodes.ABoolean810)
                                                                                                                                                                objectType.ABoolean810 = true;
                                                                                                                                                            else if (opcode == ObjectTypeOpcodes.ABoolean781)
                                                                                                                                                                objectType.ABoolean781 = true;
                                                                                                                                                            else if (opcode != ObjectTypeOpcodes.AnInt823)
                                                                                                                                                            {
                                                                                                                                                                if (opcode != ObjectTypeOpcodes.AnInt773)
                                                                                                                                                                {
                                                                                                                                                                    if (opcode == ObjectTypeOpcodes.AnInts825And808)
                                                                                                                                                                    {
                                                                                                                                                                        objectType.AnInt825 = buffer.ReadUnsignedShort();
                                                                                                                                                                        objectType.AnInt808 = buffer.ReadUnsignedShort();
                                                                                                                                                                    }
                                                                                                                                                                    else if (opcode == ObjectTypeOpcodes.ABoolean835)
                                                                                                                                                                        objectType.ABoolean835 = true;
                                                                                                                                                                    else if (opcode != ObjectTypeOpcodes.AnInt813)
                                                                                                                                                                    {
                                                                                                                                                                        if (opcode == 189)
                                                                                                                                                                        {
                                                                                                                                                                        }
                                                                                                                                                                        else if (opcode >= 190 && opcode < 196)
                                                                                                                                                                        {
                                                                                                                                                                            buffer.ReadUnsignedShort();
                                                                                                                                                                        }
                                                                                                                                                                        else if (opcode == ObjectTypeOpcodes.ExtraData)
                                                                                                                                                                        {
                                                                                                                                                                            int count = buffer.ReadUnsignedByte();
                                                                                                                                                                            objectType.ExtraData = new Dictionary<int, object>(count);
                                                                                                                                                                            for (int i = 0; i < count; i++)
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
                                                                                                if (opcode == ObjectTypeOpcodes.VarpTransformWithExtra)
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