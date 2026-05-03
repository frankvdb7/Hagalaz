using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Providers;
using Raido.Common.Buffers;
using Raido.Server.Extensions;
using Hagalaz.Game.Extensions;
using Hagalaz.Services.GameWorld.Extensions;
using UpdateFlags = Hagalaz.Game.Abstractions.Model.Creatures.Npcs.UpdateFlags;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742
{
    public class NpcRenderMasksWriter : INpcRenderMasksWriter
    {
        private readonly IHitSplatRenderTypeProvider _hitRenderMasks;

        public NpcRenderMasksWriter(IHitSplatRenderTypeProvider hitRenderMasks)
        {
            _hitRenderMasks = hitRenderMasks;
        }

        public void WriteRenderMasks(ICharacter character, INpc npc, IByteBufferWriter output, bool forceSynchronize)
        {
            var updateFlag = npc.RenderInformation.UpdateFlag;

            if (forceSynchronize)
            {
                if (npc.FacedCreature != null)
                {
                    updateFlag |= UpdateFlags.FaceCreature;
                }
                if (npc.TurnedToX != -1 && npc.TurnedToY != -1)
                {
                    updateFlag |= UpdateFlags.TurnTo;
                }
                if (npc.HasDisplayName())
                {
                    updateFlag |= UpdateFlags.SetDisplayName;
                }
            }

            if ((int)updateFlag > 0xFF)
                updateFlag |= UpdateFlags.ExpandByte;
            if ((int)updateFlag > 0xFFFF)
                updateFlag |= UpdateFlags.ExpandShort;
            if ((int)updateFlag > 0xFFFFF)
                updateFlag |= UpdateFlags.ExpandInt;

            output.WriteByte((byte)updateFlag);
            if ((int)updateFlag > 0xFF)
                output.WriteByte((byte)((int)updateFlag >> 8));
            if ((int)updateFlag > 0xFFFF)
                output.WriteByte((byte)((int)updateFlag >> 16));
            if ((int)updateFlag > 0xFFFFF)
                output.WriteByte((byte)((int)updateFlag >> 24));

            if ((updateFlag & UpdateFlags.SetCombatLevel) != 0)
                output.WriteInt16BigEndian((short)npc.Statistics.CombatLevel);

            if ((updateFlag & UpdateFlags.Graphic3) != 0)
            {
                var graphic = npc.RenderInformation.GetCurrentGraphics(2);
                output.WriteInt16BigEndian((short)graphic.Id);
                output.WriteInt32MixedEndian(graphic.Delay | graphic.Height << 16);
                output.WriteByteC((byte)graphic.Rotation);
            }

            if ((updateFlag & UpdateFlags.Glow) != 0)
            {
                output.WriteByteA((byte)npc.RenderedGlow!.Red);
                output.WriteByteA((byte)npc.RenderedGlow!.Green);
                output.WriteByteC((byte)npc.RenderedGlow!.Blue);
                output.WriteByteC((byte)npc.RenderedGlow!.Alpha);
                output.WriteInt16BigEndian((short)npc.RenderedGlow!.Delay);
                output.WriteInt16BigEndian((short)(npc.RenderedGlow!.Delay + npc.RenderedGlow!.Duration)); // duration
            }

            if ((updateFlag & UpdateFlags.SetDisplayName) != 0)
                output.WriteString(npc.DisplayName);

            if ((updateFlag & UpdateFlags.Graphic2) != 0)
            {
                var graphic = npc.RenderInformation.GetCurrentGraphics(1);
                output.WriteInt16BigEndian((short)graphic.Id);
                output.WriteInt32LittleEndian(graphic.Delay | graphic.Height << 16);
                output.WriteByteS((byte)graphic.Rotation);
            }

            if ((updateFlag & UpdateFlags.Transform) != 0)
                output.WriteInt32BigEndianSmart(npc.Appearance.CompositeID);

            if ((updateFlag & UpdateFlags.NonStandardMovement) != 0)
            {
                var mov = npc.RenderedNonstandardMovement!;
                var deltaFrom = Location.GetDelta(npc.Location, mov.StartLocation);
                var deltaTo = Location.GetDelta(npc.Location, mov.EndLocation);

                output.WriteByteC((byte)deltaFrom.X);
                output.WriteByteA((byte)deltaFrom.Y);
                output.WriteByteC((byte)deltaTo.X);
                output.WriteByte((byte)deltaTo.Y);
                output.WriteInt16BigEndian((short)mov.EndSpeed);
                output.WriteInt16BigEndian((short)(mov.EndSpeed + mov.StartSpeed));
                output.WriteInt16BigEndian((short)mov.FaceDirection);
            }

            if ((updateFlag & UpdateFlags.Hits) != 0)
            {
                var splats = npc.RenderedHitSplats;
                output.WriteByteC((byte)splats.Count);
                foreach (var splat in splats)
                {
                    if (splat.FirstSplatType == HitSplatType.None)
                    {
                        output.WriteInt16BigEndianSmart(32766);
                        output.WriteByteC(0); // firstHitSplatDamage
                        output.WriteInt16BigEndianSmart((short)splat.Delay);
                        continue;
                    }

                    bool activeHit = splat.Sender == character;
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

                const int modifier = 255;
                int maxLifePoints = npc.Definition.MaxLifePoints;
                var bars = npc.RenderedHitBars;
                output.WriteByte((byte)bars.Count);
                foreach (var bar in bars)
                {
                    output.WriteInt16BigEndianSmart((short)bar.Type);
                    bool display = true;
                    output.WriteInt16BigEndianSmart((short)(display ? bar.Speed : 32767)); // speed
                    if (!display)
                    {
                        continue;
                    }

                    output.WriteInt16BigEndianSmart((short)(bar.Delay * 30)); // delay
                    var perc = bar.NewLifePoints * modifier / maxLifePoints;
                    if (perc > byte.MaxValue)
                    {
                        perc = byte.MaxValue;
                    }
                    output.WriteByteC((byte)perc);
                    if (bar.Speed > 0)
                    {
                        output.WriteByteA((byte)(bar.CurrentLifePoints * modifier / maxLifePoints));
                    }
                }
            }

            if ((updateFlag & UpdateFlags.TurnTo) != 0)
            {
                // for some reason for npcs we don't need to calculate the angle
                var baseX = npc.Viewport.BaseAbsX;
                var baseY = npc.Viewport.BaseAbsY;
                output.WriteInt16BigEndian((short)(npc.TurnedToX + baseX + baseX));
                output.WriteInt16LittleEndian((short)(npc.TurnedToY + baseY + baseY));
            }

            if ((updateFlag & UpdateFlags.Graphic1) != 0)
            {
                var graphic = npc.RenderInformation.GetCurrentGraphics(0);
                output.WriteInt16BigEndianA((short)graphic.Id);
                output.WriteInt32LittleEndian(graphic.Delay | graphic.Height << 16);
                output.WriteByteC((byte)graphic.Rotation);
            }

            if ((updateFlag & UpdateFlags.FaceCreature) != 0)
            {
                if (npc.FacedCreature == null)
                    output.WriteInt16BigEndianA(-1);
                else if (npc.FacedCreature is INpc)
                    output.WriteInt16BigEndianA((short)npc.FacedCreature.Index);
                else
                    output.WriteInt16BigEndianA((short)(npc.FacedCreature.Index + 32768));
            }

            if ((updateFlag & UpdateFlags.Speak) != 0)
                output.WriteString(npc.SpeakingText!);

            if ((updateFlag & UpdateFlags.Animation) != 0)
            {
                for (int i = 0; i < 4; i++)
                    output.WriteInt32BigEndianSmart(npc.RenderInformation.CurrentAnimation.Id);
                output.WriteByteC((byte)npc.RenderInformation.CurrentAnimation.Delay);
            }

            if ((updateFlag & UpdateFlags.Graphic4) != 0)
            {
                var graphic = npc.RenderInformation.GetCurrentGraphics(3);
                output.WriteInt16LittleEndian((short)graphic.Id);
                output.WriteInt32LittleEndian(graphic.Delay | graphic.Height << 16);
                output.WriteByteS((byte)graphic.Rotation);
            }

            /*if ((updateFlag & (int)UpdateFlags.SecondaryHPBar) != 0)
            {
                output.WriteShortA((short)renderable.RenderedSecondaryBar.Duration);
                output.WriteByteA(renderable.RenderedSecondaryBar.SomeByteValue1);
                output.WriteByteS(renderable.RenderedSecondaryBar.SomeByteValue2);
            }*/

        }
    }
}
