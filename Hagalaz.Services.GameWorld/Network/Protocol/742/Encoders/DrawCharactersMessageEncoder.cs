using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    // TODO - refactor this to use synchronization blocks
    public class DrawCharactersMessageEncoder : IRaidoMessageEncoder<DrawCharactersMessage>
    {
        public const int MaxCharactersPerCycle = 10;

        private readonly ICharacterLocationService _characterLocationMap;
        private readonly ICharacterRenderMasksWriter _characterRenderMasks;

        public DrawCharactersMessageEncoder(ICharacterLocationService characterLocationMap, ICharacterRenderMasksWriter characterRenderMasks)
        {
            _characterLocationMap = characterLocationMap;
            _characterRenderMasks = characterRenderMasks;
        }

        public void EncodeMessage(DrawCharactersMessage message, IRaidoMessageBinaryWriter output)
        {
            output.SetOpcode(90).SetSize(RaidoMessageSize.VariableShort);

            var writingFor = message.Character;
            var allCharacters = message.AllCharacters;
            var localCharacters = message.LocalCharacters;
            var rebuildedCharacters = new LinkedList<ICharacter>();
            var updateRequests = new LinkedList<ICharacter>();

            // Local updating.
            for (var nsn = 0; nsn < 2; nsn++)
            {
                var bitWriter = output.BeginBitAccess();
                var updatingIdles = nsn == 1;
                var skipCount = -1;

                // We need to check if character is suitable for
                // our current cycle.
                foreach (var localCharacter in localCharacters.Where(localCharacter => writingFor.RenderInformation.IsIdle(localCharacter.Index) == updatingIdles))
                {
                    var updateType = GetLocalUpdateType(writingFor, localCharacter, allCharacters);
                    if (updateType == -1)
                    {
                        // -1 Means we should skip this character
                        // since no update is needed.
                        skipCount++;
                        writingFor.RenderInformation.SetIdleOnThisLoop(localCharacter.Index, true);
                    }
                    else
                    {
                        // Discharge skip count first.
                        WriteSkip(skipCount, bitWriter);
                        skipCount = -1;

                        // 0 Means we are skipping,
                        // 1 means we are updating.
                        bitWriter.WriteBits(1, 1);

                        // Mark if we need flag based updating.
                        // Of course if we are removing character
                        // we can't update it with flag-based updating.
                        bitWriter.WriteBits(1, updateType == -2 ? 0 : localCharacter.RenderInformation.FlagUpdateRequired ? 1 : 0);
                        // Notify client about our update type.
                        bitWriter.WriteBits(2, updateType == -2 ? 0 : updateType);
                        // Add it to our update requests
                        if (updateType != -2 && localCharacter.RenderInformation.FlagUpdateRequired)
                        {
                            updateRequests.AddLast(localCharacter);
                        }

                        switch (updateType)
                        {
                            case -2:
                                {
                                    // We are removing player here.
                                    var playerExists = allCharacters.ContainsKey(localCharacter.Index);
                                    var globalUpdateType = GetGlobalUpdateType(localCharacter);
                                    // Check if we can update player sortHash here
                                    // since when player is removed it can be
                                    // globally updated only on next packet.
                                    if (playerExists && globalUpdateType != 0)
                                    {
                                        UpdateGlobalPlayer(writingFor, localCharacter, globalUpdateType, updateRequests, bitWriter);
                                    }
                                    else
                                    {
                                        // Inform that we are not 
                                        // doing global updating.
                                        bitWriter.WriteBits(1, 0);
                                    }

                                    writingFor.RenderInformation.SetInViewport(localCharacter.Index, false);
                                    writingFor.RenderInformation.SetJustCrossedViewport(localCharacter.Index, true);
                                    break;
                                }

                            case 0:
                                {
                                    // It is only flag-based updating ,
                                    // so we don't have anything to do here.
                                    break;
                                }

                            case 1: // Character moved
                                {
                                    // Three bits sortHash update.
                                    var delta = Location.GetDelta(localCharacter.RenderInformation.LastLocation, localCharacter.Location);
                                    bitWriter.WriteBits(3, DirectionHelper.ThreeBitsMovementType[delta.X + 1, delta.Y + 1]);
                                    break;
                                }

                            case 2: // Character ran
                                {
                                    // Four bits sortHash update.
                                    var delta = Location.GetDelta(localCharacter.RenderInformation.LastLocation, localCharacter.Location);
                                    bitWriter.WriteBits(4, DirectionHelper.FourBitsMovementType[delta.X + 2, delta.Y + 2]);
                                    break;
                                }

                            case 3:
                                {
                                    // 12 or 30 bits sortHash update.
                                    var delta = Location.GetDelta(localCharacter.RenderInformation.LastLocation, localCharacter.Location);
                                    var deltaX = delta.X < 0 ? -delta.X : delta.X;
                                    var deltaY = delta.Y < 0 ? -delta.Y : delta.Y;
                                    var deltaZ = delta.Z < 0 ? -delta.Z : delta.Z;

                                    if (deltaX <= 15 && deltaY <= 15)
                                    {
                                        // We're doing 12bits update here.
                                        bitWriter.WriteBits(1, 0);
                                        deltaX = delta.X < 0 ? delta.X + 32 : delta.X;
                                        deltaY = delta.Y < 0 ? delta.Y + 32 : delta.Y;
                                        deltaZ = delta.Z;
                                        bitWriter.WriteBits(12, (deltaY & 0x1f) | ((deltaX & 0x1f) << 5) | ((deltaZ & 0x3) << 10));
                                    }
                                    else
                                    {
                                        // We're doing 30bits update here.
                                        // First we cast position to unsigned so we don't override
                                        // sign bit.
                                        bitWriter.WriteBits(1, 1);
                                        deltaX = delta.X;
                                        deltaY = delta.Y;
                                        deltaZ = delta.Z;
                                        bitWriter.WriteBits(30, (deltaY & 0x3fff) | ((deltaX & 0x3fff) << 14) | ((deltaZ & 0x3) << 28));
                                    }
                                    break;
                                }

                            default:
                                throw new InvalidOperationException($"UpdateType {updateType} not found");
                        }
                    }
                }

                // Discharge the remaining skip count.
                WriteSkip(skipCount, bitWriter);

                bitWriter.EndBitAccess();
            }

            var addedPlayers = 0;

            // Global updating.
            for (var nsn = 0; nsn < 2; nsn++)
            {
                var bitWriter = output.BeginBitAccess();
                var updatingIdles = nsn == 0;
                var skipCount = -1;

                for (var index = 1; index < 2048; index++)
                {
                    // We need to check if character is suitable for
                    // our current cycle.
                    if (writingFor.RenderInformation.IsInViewport(index) || writingFor.RenderInformation.HasJustCrossedViewport(index))
                    {
                        continue;
                    }

                    if (writingFor.RenderInformation.IsIdle(index) != updatingIdles)
                    {
                        continue;
                    }

                    // First off , we check if updatable 
                    // does not exist, if so then we skip it.
                    if (!allCharacters.TryGetValue(index, out var updatable) || updatable == null)
                    {
                        writingFor.RenderInformation.SetIdleOnThisLoop(index, true);
                        skipCount++;
                        continue;
                    }

                    // Then we check if we can render 
                    if (CanRenderFor(writingFor, updatable) && addedPlayers < MaxCharactersPerCycle)
                    {
                        // Since we are rendering it , it means
                        // that we are not skipping it.
                        // So we need to discharge skip count.
                        WriteSkip(skipCount, bitWriter);
                        skipCount = -1;

                        // Write addplayer block.
                        UpdateGlobalPlayer(writingFor, updatable, 0, updateRequests, bitWriter);
                        addedPlayers++;
                    }
                    else
                    {
                        // We can not render this character , but
                        // we still need to check if we can update
                        // it is global data.
                        var updateType = GetGlobalUpdateType(updatable);
                        if (updateType != 0)
                        {
                            // Discharge skip first
                            WriteSkip(skipCount, bitWriter);
                            skipCount = -1;

                            // We need to update it.
                            UpdateGlobalPlayer(writingFor, updatable, updateType, updateRequests, bitWriter);
                        }
                        else
                        {
                            // We can skip it.
                            writingFor.RenderInformation.SetIdleOnThisLoop(updatable.Index, true);
                            skipCount++;
                        }
                    }
                }

                // Discharge the remaining skip count.
                WriteSkip(skipCount, bitWriter);

                bitWriter.EndBitAccess();
            }

            // Now we need to rebuild the localCharacters list.
            for (var index = 1; index < 2048; index++)
            {
                if (writingFor.RenderInformation.IsInViewport(index))
                {
                    rebuildedCharacters.AddLast(allCharacters[index]);
                }
            }

            writingFor.RenderInformation.LocalCharacters = rebuildedCharacters;

            // Process update flag requests.
            foreach (var updateable in updateRequests)
            {
                _characterRenderMasks.WriteRenderMasks(updateable, output, writingFor.RenderInformation.HasJustCrossedViewport(updateable.Index));
            }
        }

        /// <summary>
        ///     Write's global player update block.
        /// </summary>
        /// <param name="writingFor">The character for who this packet are composed.</param>
        /// <param name="updatable">The character which is being global updated.</param>
        /// <param name="updateType">Type of global updating.</param>
        /// <param name="updateRequests">The update requests.</param>
        private void UpdateGlobalPlayer(ICharacter writingFor, ICharacter updatable, int updateType, LinkedList<ICharacter> updateRequests, IRaidoMessageBitWriter writer)
        {
            writer.WriteBits(1, 1);
            writer.WriteBits(2, updateType);
            switch (updateType)
            {
                case 0: // add player
                    var type = GetGlobalUpdateType(updatable);
                    if (type != 0)
                    {
                        UpdateGlobalPlayer(writingFor, updatable, type, updateRequests, writer);
                    }
                    else
                    {
                        writer.WriteBits(1, 0);
                    }

                    var deltaX = updatable.Location.X - (updatable.Location.RegionX << 6);
                    var deltaY = updatable.Location.Y - (updatable.Location.RegionY << 6);
                    writer.WriteBits(6, deltaX);
                    writer.WriteBits(6, deltaY);
                    writer.WriteBits(1, 1); // requires flag update
                    writingFor.RenderInformation.SetInViewport(updatable.Index, true);
                    writingFor.RenderInformation.SetJustCrossedViewport(updatable.Index, true);
                    writingFor.RenderInformation.SetIdleOnThisLoop(updatable.Index, true);
                    updateRequests.AddLast(updatable);
                    break;
                case 1: // Z update
                    var hashLoc = _characterLocationMap.FindLocationByIndex(updatable.Index);
                    var z = updatable.Location.Z - hashLoc.Z;
                    writer.WriteBits(2, z & 0x3);
                    break;
                case 2: // maphash update
                    hashLoc = _characterLocationMap.FindLocationByIndex(updatable.Index);
                    deltaX = hashLoc.RegionX - updatable.Location.RegionX;
                    deltaY = hashLoc.RegionY - updatable.Location.RegionY;
                    z = updatable.Location.Z - hashLoc.Z;
                    writer.WriteBits(5, ((z & 0x3) << 3) | (DirectionHelper.RegionMovementType[deltaX + 1, deltaY + 1] & 0x7));
                    break;
                case 3: // Full update.
                    hashLoc = _characterLocationMap.FindLocationByIndex(updatable.Index);
                    deltaX = hashLoc.RegionX - updatable.Location.RegionX;
                    deltaY = hashLoc.RegionY - updatable.Location.RegionY;
                    var deltaZ = hashLoc.Z - updatable.Location.Z;
                    var hash = ((deltaY & 0xFF) | ((deltaX & 0xFF) << 8) | ((deltaZ & 0x3) << 16)) & 0x3FFFF;
                    writer.WriteBits(18, hash);
                    break;
            }
        }

        /// <summary>
        ///     Write's skip block to packet.
        /// </summary>
        /// <param name="skip">The amount of indexes to skip.</param>
        /// <exception cref="System.Exception"></exception>
        private static void WriteSkip(int skip, IRaidoMessageBitWriter writer)
        {
            if (skip > -1)
            {
                var type = 0;
                if (skip != 0)
                {
                    if (skip < 32)
                    {
                        type = 1;
                    }
                    else if (skip < 256)
                    {
                        type = 2;
                    }
                    else if (skip < 2048)
                    {
                        type = 3;
                    }
                    else
                    {
                        throw new Exception("Skip count cannot be over 2047!");
                    }
                }

                writer.WriteBits(1, 0);
                writer.WriteBits(2, type);
                switch (type)
                {
                    case 1:
                        writer.WriteBits(5, skip);
                        break;
                    case 2:
                        writer.WriteBits(8, skip);
                        break;
                    case 3:
                        writer.WriteBits(11, skip);
                        break;
                }
            }
        }


        /// <summary>
        ///     Get's if 'renderable' can be rendered for 'renderingFor'.
        /// </summary>
        /// <param name="renderingFor">The character for which this packet are composed.</param>
        /// <param name="renderable">The character which needs to be rendered.</param>
        /// <returns><c>true</c> if this instance [can render for] the specified rendering for; otherwise, <c>false</c>.</returns>
        private static bool CanRenderFor(ICharacter renderingFor, ICharacter renderable) => renderingFor.Viewport.VisibleCreatures.Contains(renderable);

        /// <summary>
        ///     Get's movement update type
        ///     for given 'toCheck' character.
        /// </summary>
        /// <param name="toCheck">Character for which we are getting movement update type.</param>
        /// <returns>Returns 1 if 3 Bit's update type required, 2 if 4bits update required and 3 if other.</returns>
        private static int GetMovementUpdateType(ICharacter toCheck)
        {
            var delta = Location.GetDelta(toCheck.RenderInformation.LastLocation, toCheck.Location);
            if (delta.X == 0 && delta.Y == 0 && delta.Z == 0)
            {
                return -1;
            }

            if (delta.Z == 0)
            {
                int deltaX = delta.X < 0 ? -delta.X : delta.X;
                int deltaY = delta.Y < 0 ? -delta.Y : delta.Y;

                if (deltaX < 2 && deltaY < 2 && DirectionHelper.ThreeBitsMovementType[delta.X + 1, delta.Y + 1] != -1)
                {
                    return 1; // 3 bits
                }

                if (deltaX < 3 && deltaY < 3 && DirectionHelper.FourBitsMovementType[delta.X + 2, delta.Y + 2] != -1)
                {
                    return 2; // 4 bits
                }

                return 3; // 12 or 30
            }

            return 3; // 12 or 30
        }

        /// <summary>
        ///     Check's if the local player update
        ///     routine should be performed in packet.
        /// </summary>
        /// <param name="owner">The person to which gpi is being composed.</param>
        /// <param name="checkable">The person who needs to be checked.</param>
        /// <returns>
        ///     Returns local update type ,-2 means should be removed, -1 means not needed , 0 means should be removed , 1 means
        ///     walking update required
        ///     2 means running update required , 3 means teleport update required.
        /// </returns>
        private static int GetLocalUpdateType(ICharacter owner, ICharacter checkable, IDictionary<int, ICharacter> allCharacters)
        {
            allCharacters.TryGetValue(checkable.Index, out var character);
            if (owner != checkable && (character != checkable || !CanRenderFor(owner, checkable)))
            {
                // This check is checking
                // if we can remove player
                // so it disappears from owners screen.
                // First off we check if owner and checkingFor 
                // is not same person, because GPI doesn't support
                // removing yourself.
                return -2;
            }

            if (checkable.Movement.Teleported || checkable.Movement.Moved)
            {
                var type = GetMovementUpdateType(checkable);
                if (type != -1)
                {
                    return type;
                }
            }

            if (checkable.RenderInformation.FlagUpdateRequired)
            {
                // Only standart flag-based update required.
                return 0;
            }

            // No updates needed.
            return -1;
        }

        /// <summary>
        ///     Get's global update type.
        /// </summary>
        /// <param name="toUpdate">To update.</param>
        private int GetGlobalUpdateType(ICharacter toUpdate)
        {
            var location = _characterLocationMap.FindLocationByIndex(toUpdate.Index);
            var deltaX = location.RegionX - toUpdate.Location.RegionX;
            var deltaY = location.RegionY - toUpdate.Location.RegionY;
            var deltaZ = location.Z - toUpdate.Location.Z;
            if (deltaX == 0 && deltaY == 0 && deltaZ == 0)
            {
                return 0; // no update needed
            }

            if (deltaX == 0 && deltaY == 0 && deltaZ != 0)
            {
                return 1; // z update requed
            }

            if (deltaX >= -1 && deltaX <= 1 && deltaY >= -1 && deltaY <= 1 && DirectionHelper.RegionMovementType[deltaX + 1, deltaY + 1] != -1)
            {
                return 2; // map direction update requed
            }

            return 3; // full update required
        }
    }
}
