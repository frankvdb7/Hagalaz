using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    // TODO - refactor this to use synchronization blocks
    public class DrawNpcsMessageEncoder : IRaidoMessageEncoder<DrawNpcsMessage>
    {
        private readonly INpcRenderMasksWriter _masksWriter;

        public DrawNpcsMessageEncoder(INpcRenderMasksWriter masksWriter) => _masksWriter = masksWriter;

        public void EncodeMessage(DrawNpcsMessage message, IRaidoMessageBinaryWriter output)
        {
            output.SetOpcode(message.IsLargeSceneView ? 65 : 72).SetSize(RaidoMessageSize.VariableShort);
            EncodeCoreBlock(message, output);
        }

        private void EncodeCoreBlock(DrawNpcsMessage message, IRaidoMessageBinaryWriter output)
        {
            var writingFor = message.Character;
            var localNpcs = message.LocalNpcs;
            var visibleNpcs = message.VisibleNpcs;
            var updateRequests = new LinkedList<INpc>();
            var addedNpcs = new List<INpc>();
            var removedNpcs = new List<INpc>();

            var bitWriter = output.BeginBitAccess();
            bitWriter.WriteBits(8, localNpcs.Count);

            foreach (var npc in localNpcs)
            {
                if (visibleNpcs.Contains(npc) && NeedsUpdateCycle(npc))
                {
                    var needsFlagUpdate = NeedsFlagUpdating(npc);
                    UpdateCycle(npc, bitWriter, needsFlagUpdate);

                    if (needsFlagUpdate)
                    {
                        updateRequests.AddLast(npc);
                    }
                } 
                else
                {
                    // Remove this npc, as it doesn't meet local standards.
                    removedNpcs.Add(npc);

                    // Signify the client that this npc needs to be removed.
                    bitWriter.WriteBits(1, 1);
                    bitWriter.WriteBits(2, 3);
                }
            }

            removedNpcs.ForEach(npc => localNpcs.Remove(npc));

            // Now we need to rebuild the localNpcs list.
            foreach (var npc in visibleNpcs)
            {
                // We cannot have more than 255 npcs in the client simultaneously.
                if (localNpcs.Count >= 255)
                {
                    break;
                }
                // Check if we already added the npc in a previous cycle,
                // if so skip this npc, because it was already added previously
                if (localNpcs.Contains(npc))
                {
                    continue;
                }
                localNpcs.AddLast(npc);
                updateRequests.AddLast(npc);
                addedNpcs.Add(npc);
                EncodeAddNewNpcBlock(writingFor, npc, bitWriter);
            }

            // Finalize the composition.
            if (addedNpcs.Any() || updateRequests.Any())
            {
                bitWriter.WriteBits(15, 32767);
            }

            bitWriter.EndBitAccess();

            writingFor.RenderInformation.LocalNpcs = localNpcs;

            // Process update flag requests.
            foreach (var updateable in updateRequests)
            {
                _masksWriter.WriteRenderMasks(writingFor, updateable, output, addedNpcs.Contains(updateable));
            }
        }

        private static void EncodeAddNewNpcBlock(ICharacter character, INpc npc, IRaidoMessageBitWriter output)
        {
            output.WriteBits(15, npc.Index);
            output.WriteBits(1, npc.Movement.Teleported ? 1 : 0);

            var largeSceneView = character.RenderInformation.LargeSceneView;
            var xDelta = npc.Location.X - character.Location.X;
            var yDelta = npc.Location.Y - character.Location.Y;
            if (largeSceneView)
            {
                if (xDelta < 127)
                {
                    xDelta += 256;
                }

                if (yDelta < 127)
                {
                    yDelta += 256;
                }
            }
            else
            {
                if (xDelta < 0)
                {
                    xDelta += 32;
                }

                if (yDelta < 0)
                {
                    yDelta += 32;
                }
            }

            var faceDirection = npc.FaceDirection;
            output.WriteBits(3, DirectionHelper.GetNpcMovementType(faceDirection.GetDeltaX(), faceDirection.GetDeltaY()));
            output.WriteBits(2, npc.Location.Z);
            output.WriteBits(largeSceneView ? 8 : 5, yDelta);
            output.WriteBits(15, npc.Appearance.CompositeID);
            output.WriteBits(1, 1); // force flag mask update
            output.WriteBits(largeSceneView ? 8 : 5, xDelta);
        }

        private static void UpdateCycle(INpc npc, IRaidoMessageBitWriter output, bool needFlagUpdate)
        {
            var delta = Location.GetDelta(npc.RenderInformation.LastLocation, npc.Location);
            if (!npc.Movement.Moved || delta.X == 0 && delta.Y == 0 && delta.Z == 0)
            {
                if (!needFlagUpdate)
                {
                    output.WriteBits(1, 0);
                }
                else
                {
                    output.WriteBits(1, 1);
                    output.WriteBits(2, 0);
                }
            }
            else
            {
                var type = npc.Movement.MovementType;
                if (npc.Movement.TemporaryMovementTypeEnabled)
                {
                    type = npc.Movement.LastTemporaryMovementType;
                }

                output.WriteBits(1, 1); // update needed

                if (type == MovementType.Walk)
                {
                    output.WriteBits(2, 1); // normal walk update
                    output.WriteBits(3, DirectionHelper.ThreeBitsNpcMovementType[delta.X + 1, delta.Y + 1]);
                }
                else if (type == MovementType.WalkingBackwards)
                {
                    output.WriteBits(2, 2); // special walk update
                    output.WriteBits(1, 0); // backwards walk update
                    output.WriteBits(3, DirectionHelper.ThreeBitsNpcMovementType[delta.X + 1, delta.Y + 1]);
                }
                else if (type == MovementType.Run)
                {
                    output.WriteBits(2, 2); // special walk update
                    output.WriteBits(1, 1); // run walk update

                    var deltaX = delta.X;
                    var deltaY = delta.Y;
                    output.WriteBits(3, DirectionHelper.GetNpcMovementType(deltaX, deltaY));
                    if (deltaX < 0)
                    {
                        deltaX++;
                    }
                    else if (deltaX > 0)
                    {
                        deltaX--;
                    }

                    if (deltaY < 0)
                    {
                        deltaY++;
                    }
                    else if (deltaY > 0)
                    {
                        deltaY--;
                    }

                    output.WriteBits(3, DirectionHelper.GetNpcMovementType(deltaX, deltaY));
                }
                else
                {
                    throw new Exception("Unsupported movement type:" + npc.Movement.MovementType);
                }

                output.WriteBits(1, needFlagUpdate ? 1 : 0); // flag update needed
            }
        }

        private static bool CanUpdateMovement(INpc npc)
        {
            var type = npc.Movement.MovementType;
            if (npc.Movement.TemporaryMovementTypeEnabled)
            {
                type = npc.Movement.LastTemporaryMovementType;
            }

            if (npc.Movement.Teleported || (int)type <= 0 || (int)type >= 4)
            {
                return false;
            }

            int size = 1;
            if (type == MovementType.Run)
            {
                size = 2;
            }

            var delta = Location.GetDelta(npc.RenderInformation.LastLocation, npc.Location);
            return delta.Z == 0 && delta.X <= size && delta.X >= -size && delta.Y <= size && delta.Y >= -size;
        }

        private static bool NeedsUpdateCycle(INpc npc)
        {
            if (npc.Movement.Teleported || !CanUpdateMovement(npc))
            {
                return false;
            }

            return true;
        }

        private static bool NeedsFlagUpdating(INpc npc) => npc.RenderInformation.FlagUpdateRequired;

    }
}
