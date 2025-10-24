using System;
using Hagalaz.Cache.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Services.GameWorld.Store;
using Hagalaz.Game.Extensions;
using Hagalaz.Services.GameWorld.Extensions;
using Raido.Server.Extensions;
using Raido.Common.Buffers;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742
{
    public class CharacterRenderMasksWriter : ICharacterRenderMasksWriter
    {
        private readonly ItemStore _itemStore;
        private readonly IHitSplatRenderTypeProvider _hitRenderMasks;
        private readonly IBodyDataRepository _bodyDataRepository;
        private readonly IClientMapDefinitionProvider _clientMapDefinitionProvider;

        public CharacterRenderMasksWriter(ItemStore itemStore, IHitSplatRenderTypeProvider hitRenderMasks, IBodyDataRepository bodyDataRepository, IClientMapDefinitionProvider clientMapDefinitionProvider)
        {
            _itemStore = itemStore;
            _hitRenderMasks = hitRenderMasks;
            _bodyDataRepository = bodyDataRepository;
            _clientMapDefinitionProvider = clientMapDefinitionProvider;
        }

        public void WriteAppearance(ICharacter character, IByteBufferWriter output, bool forceSynchronize)
        {
            // appearance header
            var beforeAppearanceBlockLength = output.Length;
            var appearanceLength = output.GetSpan(1);
            output.Advance(1);

            var skillArea = !character.Area.Script.ShouldRenderBaseCombatLevel(character) || character.Appearance.IsTransformedToNpc();
            var bitFlag = 0;
            bitFlag |= character.Appearance.Female ? 0x1 : 0;
            bitFlag |= character.HasDisplayName() ? 0x2 : 0;
            bitFlag |= skillArea ? 0x4 : 0;
            bitFlag |= ((character.Appearance.Size - 1) << 3) & 0x7;
            var title = (byte)character.Appearance.DisplayTitle;
            if (title > 0 && title < 32)
                bitFlag |= 0x40; // prepend title
            if (title >= 32 && title <= 38)
                bitFlag |= 0x80; // append title
            output.WriteByte((byte)bitFlag);

            if (title > 0 && title < 32)
            {
                var def = _clientMapDefinitionProvider.Get(character.Appearance.Female ? 3872 : 1093);
                if (def != null)
                {
                    output.WriteString(def.GetStringValue(title), true);
                }
            }
            if (title >= 32 && title <= 38)
            {
                var def = _clientMapDefinitionProvider.Get(character.Appearance.Female ? 3872 : 1093);
                if (def != null)
                {
                    output.WriteString(def.GetStringValue(title), true);
                }
            }

            if (character.Appearance.IsTransformedToNpc())
                output.WriteByte(unchecked((byte)-1));
            else
                output.WriteByte((byte)character.Appearance.SkullIcon);

            output.WriteByte((byte)character.Appearance.PrayerIcon);
            output.WriteByte((byte)(character.Appearance.Visible ? 0 : 1)); // isInvisible ( if it's invisible players with rights >= 2 can still see it )

            if (character.Appearance.IsTransformedToNpc())
            {
                output.WriteInt16BigEndian(-1); // mark as npc.
                output.WriteInt32BigEndianSmart(character.Appearance.NpcId);

                var teamID = 0;
                for (var i = 0; i < _bodyDataRepository.BodySlotCount; i++)
                {
                    if (_bodyDataRepository.IsDisabledSlot((BodyPart)i))
                    {
                        continue;
                    }

                    var part = character.Appearance.GetDrawnBodyPart((BodyPart)i);
                    if (part < 0x4000)
                    {
                        continue;
                    }

                    var itemID = part - 0x4000; // 16384
                    var definition = _itemStore.GetOrAdd(itemID);
                    if (definition.TeamId != 0)
                        teamID = definition.TeamId;
                }

                output.WriteByte((byte)teamID);
            }
            else
            {
                var itemAppearanceHash = 0;
                var count = 0;
                for (var i = 0; i < _bodyDataRepository.BodySlotCount; i++)
                {
                    var part = (BodyPart)i;
                    if (_bodyDataRepository.IsDisabledSlot(part))
                    {
                        continue;
                    }

                    var bodyPart = character.Appearance.GetDrawnBodyPart(part);
                    if (bodyPart == 0)
                        output.WriteByte(0);
                    else
                        output.WriteInt16BigEndian((short)bodyPart);

                    if (character.Appearance.GetDrawnItemPart(part) != null)
                        itemAppearanceHash |= 1 << count;

                    count++;
                }

                if (character.RenderInformation.ItemAppearanceUpdateRequired || forceSynchronize)
                {
                    output.WriteInt16BigEndian((short)itemAppearanceHash); // recolour mask
                    WriteItemAppearance(character, output);
                }
                else
                {
                    output.WriteInt16BigEndian(0); // recolour mask
                }
            }

            for (int i = 0; i < 10; i++)
                output.WriteByte((byte)character.Appearance.GetColor((ColorType)i));

            output.WriteInt16BigEndian((short)character.Appearance.RenderId);
            output.WriteString(character.Appearance.IsTransformedToNpc() ? character.Appearance.PnpcScript.GetDisplayName() : character.DisplayName);

            var fullCombatLevel = character.Appearance.IsTransformedToNpc() ? character.Appearance.PnpcScript.GetCombatLevel() : character.Statistics.FullCombatLevel;
            output.WriteByte((byte)(skillArea ? fullCombatLevel : character.Statistics.BaseCombatLevel));
            // if area is wilderness like and character does not use summoning
            if (skillArea)
            {
                output.WriteInt16BigEndian(0); // skill level
            }
            else
            {
                output.WriteByte((byte)fullCombatLevel);
                output.WriteByte((byte)character.Area.Script.GetPvPCombatLevelRange(character)); // wildy level ( attackable levels range )
            }

            //if (renderable.Appereance.TransformedToNPC)
            //{
            // NPCDefinition definition = NPCDefinition.GetNPCDefinition(Engine.Cache, renderable.Appereance.NPCID);
            //block.AppendByte((byte)definition.anInt1504); // enable custom passive anims block
            //block.AppendShort(0); // unknown
            // block.AppendShort(0); // unknown
            //block.AppendShort(0); // unknown
            // block.AppendShort(0); // run animation
            //block.AppendByte((byte)definition.anInt1456);

            //}
            //else
            //{
            output.WriteByte(0); // > 0 enables custom passive anims block
            // }


            // appearance header
            // the client uses this byte to determine how big the appearance buffer is
            // substract 1 byte for this value
            var appearanceBlockLength = (byte)(output.Length - beforeAppearanceBlockLength - 1);
            // written as a 'ByteS' value
            appearanceLength[0] = (byte)(128 - appearanceBlockLength); 
        }

        public void WriteItemAppearance(ICharacter character, IByteBufferWriter output)
        {
            for (var slot = 0; slot < _bodyDataRepository.BodySlotCount; slot++)
            {
                var part = (BodyPart)slot;
                if (_bodyDataRepository.IsDisabledSlot(part))
                {
                    continue;
                }

                var ia = character.Appearance.GetDrawnItemPart(part);
                if (ia != null)
                {
                    output.WriteByte((byte)ia.Flags);

                    var definition = _itemStore.GetOrAdd(ia.ItemId);
                    if (ia.Flags.HasFlag(ItemUpdateFlags.Model))
                    {
                        output.WriteInt32BigEndianSmart(ia.MaleModels[0]); // male worn model1
                        output.WriteInt32BigEndianSmart(ia.FemaleModels[0]); // female worn model1
                        if (definition.MaleWornModelId2 != -1 || definition.FemaleWornModelId2 != -1)
                        {
                            output.WriteInt32BigEndianSmart(ia.MaleModels[1]);
                            output.WriteInt32BigEndianSmart(ia.FemaleModels[1]);
                        }

                        if (definition.MaleWornModelId3 != -1 || definition.FemaleWornModelId3 != -1)
                        {
                            output.WriteInt32BigEndianSmart(ia.MaleModels[2]);
                            output.WriteInt32BigEndianSmart(ia.FemaleModels[2]);
                        }
                    }

                    if (ia.Flags.HasFlag(ItemUpdateFlags.Color))
                    {
                        int modelParts = 0;
                        byte flag = 0;
                        for (int index = 0; index < 4; index++)
                        {
                            if (ia.ModelColors[index] != definition.ModifiedModelColors[index])
                                modelParts |= index << flag; // part update required
                            else
                                modelParts |= 15 << flag; // no part update required
                            flag += 4;
                        }

                        output.WriteInt16BigEndian((short)modelParts);
                        for (int index = 0; index < 4; index++)
                            if (ia.ModelColors[index] != definition.ModifiedModelColors[index])
                                output.WriteInt16BigEndian((short)ia.ModelColors[index]);
                    }

                    if (ia.Flags.HasFlag(ItemUpdateFlags.Texture))
                    {
                        int modelParts = 0;
                        byte flag = 0;
                        for (var index = 0; index < 2; index++)
                        {
                            if (ia.TextureColors[index] != definition.ModifiedTextureColors[index])
                                modelParts |= index << flag; // part update required
                            else
                                modelParts |= 15 << flag; // no part update required
                            flag += 4;
                        }

                        output.WriteByte((byte)modelParts);
                        for (var index = 0; index < 2; index++)
                            if (ia.TextureColors[index] != definition.ModifiedTextureColors[index])
                                output.WriteInt16BigEndian((short)ia.TextureColors[index]);
                    }
                }
            }
        }

        public void WriteRenderMasks(ICharacter character, IByteBufferWriter output, bool forceSynchronize)
        {
            var updateFlag = character.RenderInformation.UpdateFlag;

            if (forceSynchronize)
            {
                updateFlag |= Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Appearance;
                updateFlag |= Game.Abstractions.Model.Creatures.Characters.UpdateFlags.MovementType;
                updateFlag |= Game.Abstractions.Model.Creatures.Characters.UpdateFlags.FaceCreature;
                updateFlag |= Game.Abstractions.Model.Creatures.Characters.UpdateFlags.TurnTo;
            }

            if ((int)updateFlag > 0xFF)
                updateFlag |= Game.Abstractions.Model.Creatures.Characters.UpdateFlags.ExpandByte;
            if ((int)updateFlag > 0xFFFF)
                updateFlag |= Game.Abstractions.Model.Creatures.Characters.UpdateFlags.ExpandShort;

            output.WriteByte((byte)updateFlag);
            if ((int)updateFlag > 0xFF)
                output.WriteByte((byte)((int)updateFlag >> 8));
            if ((int)updateFlag > 0xFFFF)
                output.WriteByte((byte)((int)updateFlag >> 16));

            if (updateFlag.HasFlag(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Appearance))
                WriteAppearance(character, output, forceSynchronize);

            if (updateFlag.HasFlag(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Glow))
            {
                output.WriteByteC((byte)character.RenderedGlow!.Red);
                output.WriteByteC((byte)character.RenderedGlow!.Green);
                output.WriteByteA((byte)character.RenderedGlow!.Blue);
                output.WriteByteA((byte)character.RenderedGlow!.Alpha);
                output.WriteInt16LittleEndianA((short)character.RenderedGlow!.Delay);
                output.WriteInt16LittleEndianA((short)(character.RenderedGlow!.Delay + character.RenderedGlow!.Duration)); // duration
            }

            if (updateFlag.HasFlag(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Graphic3))
            {
                var graphic = character.RenderInformation.GetCurrentGraphics(2);
                output.WriteInt16BigEndian((short)graphic.Id);
                output.WriteInt32MiddleEndian(graphic.Delay | graphic.Height << 16);
                var settings = 0;
                settings |= graphic.Rotation & 0x7;
                //settings |= value << 3 & 0xf;
                //settings |= bool << 7 & 0x1;
                output.WriteByteA((byte)settings);
            }

            if (updateFlag.HasFlag(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.FaceCreature))
            {
                if (character.FacedCreature == null)
                    output.WriteInt16LittleEndian(-1);
                else if (character.FacedCreature is INpc)
                    output.WriteInt16LittleEndian((short)character.FacedCreature.Index);
                else
                    output.WriteInt16LittleEndian((short)(character.FacedCreature.Index + 32768));
            }

            if (updateFlag.HasFlag(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Graphic4))
            {
                var graphic = character.RenderInformation.GetCurrentGraphics(3);
                output.WriteInt16LittleEndian((short)graphic.Id);
                output.WriteInt32BigEndian(graphic.Delay | graphic.Height << 16);
                int settings = 0;
                settings |= graphic.Rotation & 0x7;
                //settings |= value << 3 & 0xf;
                //settings |= bool << 7 & 0x1;
                output.WriteByteA((byte)settings);
            }

            if (updateFlag.HasFlag(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Hits))
            {
                var splats = character.RenderedHitSplats;
                output.WriteByteA((byte)splats.Count);
                foreach (var splat in splats)
                {
                    if (splat.FirstSplatType == HitSplatType.None)
                    {
                        output.WriteInt16BigEndianSmart(32766);
                        output.WriteByteA(0); // firstHitSplatDamage
                        output.WriteInt16BigEndianSmart((short)splat.Delay);
                        continue;
                    }

                    var activeHit = splat.Sender == character;
                    if (splat.FirstSplatType != HitSplatType.None && splat.SecondSplatType == HitSplatType.None)
                    {
                        output.WriteInt16BigEndianSmart((short)_hitRenderMasks.GetRenderType(splat.FirstSplatType, activeHit, splat.FirstSplatCritical));
                        output.WriteInt16BigEndianSmart((short)splat.FirstSplatDamage);
                        output.WriteInt16BigEndianSmart((short)splat.Delay);
                        continue;
                    }

                    output.WriteInt16BigEndianSmart(32767);
                    output.WriteInt16BigEndianSmart((short)_hitRenderMasks.GetRenderType(splat.FirstSplatType, activeHit, splat.FirstSplatCritical));
                    output.WriteInt16BigEndianSmart((short)splat.FirstSplatDamage);
                    output.WriteInt16BigEndianSmart((short)_hitRenderMasks.GetRenderType(splat.SecondSplatType, activeHit, splat.SecondSplatCritical));
                    output.WriteInt16BigEndianSmart((short)splat.SecondSplatDamage);
                    output.WriteInt16BigEndianSmart((short)splat.Delay);
                }

                var maxLifePoints = character.Statistics.GetMaximumLifePoints();
                var bars = character.RenderedHitBars;
                output.WriteByte((byte)bars.Count);
                foreach (var bar in bars)
                {
                    output.WriteInt16BigEndianSmart((short)bar.Type);
                    const bool display = true;
                    output.WriteInt16BigEndianSmart((short)(display ? bar.Speed : 32767)); // speed
                    if (!display)
                    {
                        continue;
                    }

                    output.WriteInt16BigEndianSmart((short)(bar.Delay * 30)); // delay
                    var newPercentage = bar.NewLifePoints * byte.MaxValue / maxLifePoints;
                    if (newPercentage > byte.MaxValue)
                        newPercentage = byte.MaxValue;
                    output.WriteByteC((byte)newPercentage);
                    if (bar.Speed <= 0)
                    {
                        continue;
                    }

                    int oldPercentage = bar.CurrentLifePoints * byte.MaxValue / maxLifePoints;
                    if (oldPercentage > byte.MaxValue)
                        oldPercentage = byte.MaxValue;
                    output.WriteByteS((byte)oldPercentage);
                }
            }

            if (updateFlag.HasFlag(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Animation))
            {
                for (int i = 0; i < 4; i++)
                    output.WriteInt32BigEndianSmart(character.RenderInformation.CurrentAnimation.Id);
                output.WriteByteA((byte)character.RenderInformation.CurrentAnimation.Delay);
            }

            if (updateFlag.HasFlag(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.MovementType))
                output.WriteByteS((byte)character.Movement.MovementType);

            if (updateFlag.HasFlag(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Speak))
                output.WriteString(character.SpeakingText!);

            if (updateFlag.HasFlag(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.NonStandardMovement))
            {
                var mov = character.RenderedNonstandardMovement!;
                var deltaFrom = Location.GetDelta(character.Location, mov.StartLocation);
                var deltaTo = Location.GetDelta(character.Location, mov.EndLocation);

                output.WriteByte((byte)deltaFrom.X);
                output.WriteByteS((byte)deltaFrom.Y);
                output.WriteByteC((byte)deltaTo.X);
                output.WriteByteC((byte)deltaTo.Y);
                output.WriteInt16BigEndian((short)mov.EndSpeed);
                output.WriteInt16LittleEndian((short)(mov.EndSpeed + mov.StartSpeed));
                output.WriteInt16LittleEndian((short)mov.FaceDirection);
            }

            if (updateFlag.HasFlag(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.TurnTo))
            {
                // we need to calculate angle here.
                var renderableMapX = -1;
                var renderableMapY = -1;
                character.Viewport.GetLocalPosition(character.Location, ref renderableMapX, ref renderableMapY);
                double tileX = renderableMapX * 512 + character.Size * 256;
                double tileY = renderableMapY * 512 + character.Size * 256;
                double targetTileX = character.TurnedToX * 256;
                double targetTileY = character.TurnedToY * 256;
                double deltaX = tileX - targetTileX;
                double deltaY = tileY - targetTileY;
                var angle = 0;
                if (deltaX != 0 || deltaY != 0)
                    angle = (int)(Math.Atan2(deltaX, deltaY) * 2607.5945876176133) & 0x3fff;
                output.WriteInt16LittleEndian((short)angle);
            }

            if (updateFlag.HasFlag(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Graphic2))
            {
                var graphic = character.RenderInformation.GetCurrentGraphics(1);
                output.WriteInt16LittleEndianA((short)graphic.Id);
                output.WriteInt32MiddleEndian(graphic.Delay | graphic.Height << 16);
                var settings = 0;
                settings |= graphic.Rotation & 0x7;
                //settings |= value << 3 & 0xf;
                //settings |= bool << 7 & 0x1;
                output.WriteByteS((byte)settings);
            }

            if (updateFlag.HasFlag(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.TemporaryMovementType))
                output.WriteByteC((byte)character.Movement.LastTemporaryMovementType);

            if (updateFlag.HasFlag(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Graphic1))
            {
                var graphic = character.RenderInformation.GetCurrentGraphics(0);
                output.WriteInt16BigEndianA((short)graphic.Id);
                output.WriteInt32BigEndian(graphic.Delay | graphic.Height << 16);
                int settings = 0;
                settings |= graphic.Rotation & 0x7;
                //settings |= value << 3 & 0xf;
                //settings |= bool << 7 & 0x1;
                output.WriteByteC((byte)settings);
            }

            //if ((updateFlag & (int)UpdateFlags.OrangeMinimapDot) != 0)
            //packet.WriteByteS(renderable.MinimapDot == MinimapDot.OrangeDot ? (byte)1 : (byte)0);

            /*if ((updateFlag & (int)UpdateFlags.SecondaryHPBar) != 0)
            {
                packet.AppendLEShort((short)renderable.RenderedSecondaryBar.Duration);
                packet.WriteByteC(renderable.RenderedSecondaryBar.SomeByteValue1);
                packet.WriteByteA(renderable.RenderedSecondaryBar.SomeByteValue2);
            }*/

            //if ((updateFlag & (int)UpdateFlags.PWordMinimapDot) != 0)
            //packet.WriteByte(renderable.MinimapDot == MinimapDot.PWordDot ? (byte)1 : (byte)0);
        }
    }
}
